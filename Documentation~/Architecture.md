# Lumiscore Architecture & Design Intent

This document records the architectural boundaries, invariants, and trade-offs behind **Lumiscore** (`com.turingcat.lumiscore`). It focuses on *why* the system is structured the way it is so both human contributors and AI agents can extend it without violating core constraints.

---

## 1. Layered Assembly Boundaries

Lumiscore is split into four layers with strict one-way dependencies:

```
Lumiscore.Core       (Runtime/Core)      — Lighting-agnostic show data, presets, tempo map, mixer & driver contracts. Zero Unity.Timeline dependency.
       ▲
Lumiscore.Lighting   (Runtime/Lighting)  — Light node hierarchy, typed tracks/clips/effects, zero-GC evaluation controller.
       ▲
Lumiscore.Timeline   (Runtime/Timeline)  — Thin Unity Timeline View assets (StageTrackAsset, StageClipAsset, StageTrackMixer).
       ▲
Lumiscore.Editor     (Editor)            — Bidirectional Timeline sync, Undo service, Preset assets, Layout appliers, and isolated StageEditorSession.
```

Key boundary rules:
- **`Lumiscore.Core` must stay lighting-agnostic** so future stage elements (subtitles, pyro, camera cues) share the exact same show, track, clip, preset, and tempo infrastructure.
- **Editor state never leaks into Runtime.** Preview snapshots, lossless scene restore on stop/reload, and layout application live exclusively in `Lumiscore.Editor` (`StageEditorSession`), keeping Runtime clean for builds and future VRChat Udon compilation.

---

## 2. Serialization Strategy: Non-Generic Shell, Strongly-Typed Core

Unity 2022.3 and Unity 6 (`6000.0+`) share a hard `[SerializeReference]` limitation: **closed/inflated generic types are not supported** as `[SerializeReference]` field types or concrete runtime instances (e.g., `LightTrack<ColorModule>` or `GenericPresetValue<float>` cannot be serialized directly under `[SerializeReference]`).

Lumiscore resolves this with a two-layer split:
- **On disk (YAML): 100% non-generic.**
  - `StageShow` implements `ISerializationCallbackReceiver` with private non-generic DTOs (`SerializedTrackData`, `SerializedClipData`) storing assembly-qualified track/module type names, `TargetNode`, timing/blend parameters, and `[SerializeReference] StageEffectBase Effect`.
  - Concrete effects inherit from `LightEffectBase<TModule> : StageEffectBase` and must be non-generic `[Serializable]` classes.
  - Preset values use concrete non-generic subclasses of `StagePresetValueBase<T>` (`FloatPresetValue`, `ColorArrayPresetValue`, `AnimationCurvePresetValue`, `GradientPresetValue`, etc.) under `[SerializeReference] IStagePresetValue`.
- **In memory (Runtime / Authoring): strongly-typed generics.**
  - Once `StageShow.OnAfterDeserialize()` reconstructs tracks, the in-memory `Tracks` list and `GetTracks(node)` dictionary hold real `LightTrack<TModule>` / `LightClip<TModule>` / `LightEffectBase<TModule>` instances.
  - All evaluation, mixing, and future static analysis/baking operate on strongly-typed `TModule` with zero boxing.

---

## 3. Node Hierarchy & Uniform Evaluation Rule

```
StageNodeBase (Core abstract: Id, Name)
 └── LightNodeBase (Lighting abstract: RootTransform, Index, Count, Children, Layout)
      ├── LightGroupNode (Group: owns _children and _layout; never references scene MonoBehaviours)
      └── LightLeafNode  (Leaf fixture endpoint: empty Children, null Layout)
```

### Design Invariants
- **Every track evaluates its node's `Children`, never the node itself.**
  - A group track on a `LightGroupNode` evaluates across its direct children (`Count = N`, each child carrying its `Index` and sibling `Count`), then passes each child's resulting `ILightState` down the subtree.
  - A single-light override is modeled uniformly by wrapping a `LightLeafNode` in a 1-child `LightGroupNode` (`LightGroupNode.WrapSingleLight`). There are zero `if (node is LightGroupNode)` branches in the evaluation loop.
  - Recursion terminates when `node.Children.Count == 0` (`LightLeafNode`), flushing the final `ILightState` to the bound `StageDriverBase`.
- **Homogeneous `TState` per light tree.**
  - Each root light tree represents one fixture state type (e.g., moving heads vs. lasers live in separate root trees). `LightNodeBase` intentionally does **not** carry `TState` metadata; scene bindings / `StageController` own the node-to-`TState` and driver mapping.
- **Scene Hierarchy is the single source of truth for stage topology.**
  - `StageShowAsset` stores **only** song-level authoring data (`TempoMap`, tracks keyed by `TargetNodeId`, clips, and effects) — never a duplicate copy of the stage node tree.
  - `StageController` builds its runtime in-memory `RootNodes` (`LightGroupNode` / `LightLeafNode`) tree directly from scene `LightGroup` / `LightFixture` components, so editing lights in the stage immediately applies to all songs sharing that stage.
- **Zero-GC hot loop.**
  - `StageShow` maintains an in-memory `[NonSerialized] Dictionary<string, List<IStageTrack>>` for `O(1)` `GetTracks(nodeId)` lookup without per-frame re-indexing.
  - `StageController` pre-allocates per-node `ILightState` working/previous buffers once when cache is rebuilt, resetting and copying via `ILightState.ResetToDefault()` and `ILightState.CopyFrom(source)`.
  - `LightTrack<TModule>` pre-allocates reusable `TModule[]` scratch buffers and double-buffers mixer outputs so `from` and `result` never alias.
  - `LightEffectBase<TModule>.Evaluate(LightEffectContext context, TModule[] results)` is a **stateless pure function** that writes directly into the pre-allocated `results` array (0 allocations per frame, arbitrary Timeline scrubbing).
- **Effect cloning (`StageEffectBase.Clone`).**
  - Used only when duplicating clips in the editor (e.g. Ctrl+D). Implemented via pure recursive reflection with cycle protection: deep-copies arrays, `AnimationCurve`, `Gradient`, and nested objects while preserving `UnityEngine.Object` references intact (never using `JsonUtility`).

---

## 4. Mixers, Dual-End Easing, Crossfades, and Tempo Map

- **`IStageModuleMixer<TModule>` / `ILightModuleMixer<TModule>`**:
  - Separates stateless blending/easing algorithms from pure-data `TModule` classes (C++ policy/traits style).
  - Assumes 1-to-1 auto-discovery per `TModule` via `StageTypeRegistry` (`TypeCache` in Editor, assembly scan fallback in Runtime).
  - Owns three zero-GC operations writing into pre-allocated `result`:
    - `Blend(from, to, weight, mode, result)` — layer/track composition (`Override`, `Additive`, `Subtract`, `Max`, `Min`, `Multiply`).
    - `EaseIn(from, to, normalizedTime, result)` — clip entry transition (`normalizedTime` in `0..1`, `0` = `from`, `1` = `to`).
    - `EaseOut(from, to, normalizedTime, result)` — clip exit transition (`normalizedTime` in `0..1`, `0` = `to`, `1` = `from`).
  - Keeping `EaseIn` / `EaseOut` module-aware allows per-field semantics (e.g., fading intensity without interpolating discrete Gobo indices).
- **Three-phase clip timing & proportional clamping**:
  - Follows Unity Timeline's `ConformEaseValues` rule: if `EaseInDuration + EaseOutDuration > Duration`, both are scaled proportionally so entry and exit phases never overlap.
  - Overlapping clips on the same track crossfade directly via `mixer.EaseIn(clipA, clipB, overlapProgress, result)` before blending into the base state, avoiding mid-transition brightness dips.
- **Seconds on disk, Beats in context (`StageTempoMap`)**:
  - Clips and Timeline store absolute time in **seconds** for exact audio/Timeline alignment and future variable-BPM support.
  - `StageShow.TempoMap` projects `ElapsedBeat` and `CurrentBpm` into `LightEffectContext` so rhythmic effects stay BPM-portable across songs while physical effects can read `ElapsedTime`.

---

## 5. Timeline Authoring & Preview Loop

- **Uniform `GroupTrack` nesting (no `ILayerable`)**:
  - Unity Timeline's `ILayerable` is hardcoded to a single sub-track level. Lumiscore maps every node uniformly to a `GroupTrack` (which supports arbitrary nesting) with `StageTrackAsset` children underneath.
- **Bidirectional View ↔ Model sync**:
  - `StageShowAsset` is the single source of truth; Unity Timeline assets are a synchronized editing view.
  - `StageTimelineSync` guards against feedback loops (`IsSyncing` + `TimeEpsilon`), distinguishes mid-drag clip reparenting from deletion, and hooks `Undo.undoRedoPerformed`.
  - `StageShowEditService` wraps programmatic edits in unified Undo groups.
- **Single-evaluation preview loop**:
  - `StageTrackMixer.ProcessFrame` forwards `(timelineTime, info.frameId)` to `StageController.EvaluateFrame`, deduplicating multiple tracks so the show evaluates exactly once per frame in both EditMode scrubbing and PlayMode.
  - Serialized scene property restoration in EditMode preview is delegated to Unity Timeline's native `IPropertyCollector` via `StageTrackAsset.GatherProperties` → `StageDriverBase.GatherPreviewProperties(fixtureRoot, IStagePropertyCollector)` (defaulting to child `Transform`s and `Light`s, overridable per driver).
  - `StageEditorSession` listens to `StageController.AnyPreviewStarted` / `AnyPreviewStopped` to invoke optional `StageDriverBase.ResetPreview` for non-serialized runtime render state (such as `MaterialPropertyBlock`) on stop, assembly reload, or PlayMode entry.

---

## 6. Preset Framework

- **Streaming anchor attributes**:
  - `[StagePresetPart(key, displayName)]` marks the start of a named parameter group; all subsequent `public` instance fields belong to that part until another anchor or `[StagePresetIgnore]` appears.
  - `StagePresetSchema` scans `public` fields only in Base → Derived declaration (`MetadataToken`) order and merges repeated keys into the same part.
- **Many-to-many part reuse**:
  - `StagePartPresetAsset` is completely independent (no `ownerGuid`) and can be shared freely by any number of `StageEffectPresetAsset` composites.
- **Field-level overrides**:
  - `StageEffectBase.Presets` (`StagePresetManager`) tracks effect/part preset bindings and per-field overrides so applying a part preset updates non-overridden fields without clobbering manual tweaks.

---

## 7. Planned Next Layers (Reserved Extension Points)

- **Scene Hierarchy Components (`LightGroup` / `LightFixture`)**:
  - Thin scene `MonoBehaviour` components attached directly to stage `GameObject`s so authors can edit layouts and fixture-specific couplings directly in the Hierarchy, and rebuild/sync `StageShow` topology from the scene.
  - `LightLeafNode` will hold a lightweight `[NonSerialized]` runtime reference to its `LightFixture`, injected during topology sync/initialization.
- **VRChat Udon Compilation / Baking Backend**:
  - Authoring remains clean C#; a dedicated Editor-time compiler/baker will statically lower active effects, mixers, and show data into flat SoA UdonSharp / UdonAssembly without runtime reflection.
