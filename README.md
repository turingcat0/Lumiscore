# Lumiscore

A Timeline-first stage lighting authoring toolkit for Unity and VRChat.

See [Documentation~/Architecture.md](Documentation~/Architecture.md) for the full design intent, serialization invariants, evaluation pipeline, and extension boundaries.

## Package Structure

- `Runtime/Core/` — Lighting-agnostic show model (`StageShow`, `StageShowAsset`), tempo map (`StageTempoMap`), mixer/driver/effect/node base contracts, type auto-discovery (`StageTypeRegistry`), and preset runtime data (`Runtime/Core/Presets/`).
- `Runtime/Lighting/` — Light node hierarchy (`LightNodeBase`, `LightGroupNode`, `LightLeafNode`), layouts (`Line` / `Circle` / `Grid`), typed tracks/clips/effects (`LightTrack<TModule>`, `LightClip<TModule>`, `LightEffectBase<TModule>`), driver base (`LightDriverBase<TState>`), and zero-GC scene evaluation controller (`StageController`).
- `Runtime/Timeline/` — Unity Timeline integration (`StageTrackAsset`, `StageClipAsset`, `StageTrackMixer`).
- `Editor/` — Bidirectional Timeline synchronization (`StageTimelineSync`, `StageShowEditService`, `StageClipInspector`), isolated preview/layout session (`StageEditorSession`), layout appliers (`Editor/Lighting/`), and preset assets (`Editor/Presets/`).
- `Tests/Editor/` — EditMode verification tests covering YAML serialization round-trips, presets, auto-discovery, reflection cloning, and zero-GC evaluation.

## Extending Lumiscore (Minimal Example)

Implement a pure-data `ILightStateModule`, a 1-to-1 `ILightModuleMixer<TModule>`, and a stateless `LightEffectBase<TModule>`. `StageTypeRegistry` and `LightTypeCatalog` automatically discover concrete modules, mixers, and effects without manual registration.

```csharp
using System;
using Lumiscore.Core;
using Lumiscore.Lighting;
using UnityEngine;

public sealed class IntensityModule : ILightStateModule
{
    public float Value;
}

public sealed class IntensityMixer : ILightModuleMixer<IntensityModule>
{
    public void Blend(IntensityModule from, IntensityModule to, float weight, StageBlendMode mode, IntensityModule result)
    {
        result.Value = mode == StageBlendMode.Additive
            ? from.Value + to.Value * weight
            : Mathf.Lerp(from.Value, to.Value, weight);
    }

    public void EaseIn(IntensityModule from, IntensityModule to, float normalizedTime, IntensityModule result)
        => result.Value = Mathf.Lerp(from.Value, to.Value, normalizedTime);

    public void EaseOut(IntensityModule from, IntensityModule to, float normalizedTime, IntensityModule result)
        => result.Value = Mathf.Lerp(to.Value, from.Value, normalizedTime);
}

[Serializable]
public sealed class PulseIntensityEffect : LightEffectBase<IntensityModule>
{
    [StagePresetPart("timing", "Timing")]
    public float BeatsPerCycle = 2f;

    [StagePresetPart("intensity", "Intensity")]
    public float MaxIntensity = 1f;

    public override void Evaluate(LightEffectContext context, IntensityModule[] results)
    {
        float wave = 0.5f + 0.5f * Mathf.Sin(context.ElapsedBeat / Mathf.Max(0.001f, BeatsPerCycle) * Mathf.PI * 2f);
        for (int i = 0; i < results.Length; i++)
        {
            results[i].Value = wave * MaxIntensity;
        }
    }
}
```
