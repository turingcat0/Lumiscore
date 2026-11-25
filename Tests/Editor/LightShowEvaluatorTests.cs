using System;
using System.Collections.Generic;
using Lumiscore.Core;
using Lumiscore.Editor;
using Lumiscore.Lighting;
using Lumiscore.Timeline;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Timeline;

namespace Lumiscore.Tests.Editor
{
    public sealed class EvalIntensityModule : ILightStateModule
    {
        public float Value;
    }

    public sealed class EvalLightState : IHasLightModule<EvalIntensityModule>
    {
        public EvalIntensityModule Intensity = new EvalIntensityModule();

        public TModule GetModule<TModule>() where TModule : class, ILightStateModule
        {
            if (typeof(TModule) == typeof(EvalIntensityModule))
            {
                return Intensity as TModule;
            }

            return null;
        }

        public void SetModule<TModule>(TModule module) where TModule : class, ILightStateModule
        {
            if (module is EvalIntensityModule intensity)
            {
                Intensity = intensity;
            }
        }

        public void CopyFrom(ILightState source)
        {
            if (source is EvalLightState other && other.Intensity != null)
            {
                Intensity.Value = other.Intensity.Value;
            }
        }

        public void ResetToDefault()
        {
            Intensity.Value = 0f;
        }
    }

    [Serializable]
    public sealed class MockEvalLightDriver : LightDriverBase<EvalLightState>
    {
        public float CurrentIntensity = 7.5f;
        public bool ResetPreviewCalled;

        public override void ApplyState(EvalLightState state)
        {
            if (state?.Intensity != null)
            {
                CurrentIntensity = state.Intensity.Value;
            }
        }

        public override void ResetPreview(GameObject fixtureRoot)
        {
            ResetPreviewCalled = true;
            CurrentIntensity = 0f;
        }
    }

    internal sealed class MockPropertyCollector : IStagePropertyCollector
    {
        public readonly List<Component> CollectedComponents = new List<Component>();

        public void AddComponent(Component component)
        {
            if (component != null) CollectedComponents.Add(component);
        }

        public void AddProperty<TComponent>(GameObject target, string propertyName) where TComponent : Component
        {
        }

        public void AddTransformsInChildren(GameObject root)
        {
            if (root == null) return;
            CollectedComponents.AddRange(root.GetComponentsInChildren<Transform>(true));
        }
    }

    public sealed class EvalIntensityMixer : ILightModuleMixer<EvalIntensityModule>
    {
        public void Blend(
            EvalIntensityModule from,
            EvalIntensityModule to,
            float weight,
            StageBlendMode mode,
            EvalIntensityModule result)
        {
            float a = from != null ? from.Value : 0f;
            float b = to != null ? to.Value : 0f;
            switch (mode)
            {
                case StageBlendMode.Additive:
                    result.Value = a + b * weight;
                    break;
                case StageBlendMode.Multiply:
                    result.Value = Mathf.Lerp(a, a * b, weight);
                    break;
                default:
                    result.Value = Mathf.Lerp(a, b, weight);
                    break;
            }
        }

        public void EaseIn(
            EvalIntensityModule from,
            EvalIntensityModule to,
            float normalizedTime,
            EvalIntensityModule result)
        {
            float a = from != null ? from.Value : 0f;
            float b = to != null ? to.Value : 0f;
            result.Value = Mathf.Lerp(a, b, normalizedTime);
        }

        public void EaseOut(
            EvalIntensityModule from,
            EvalIntensityModule to,
            float normalizedTime,
            EvalIntensityModule result)
        {
            float a = from != null ? from.Value : 0f;
            float b = to != null ? to.Value : 0f;
            result.Value = Mathf.Lerp(b, a, normalizedTime);
        }
    }

    [Serializable]
    public sealed class SiblingRampEffect : LightEffectBase<EvalIntensityModule>
    {
        public float BaseValue = 10f;
        public float StepPerSibling = 10f;
        public Color[] Palette = { Color.red, Color.blue };

        public override void Evaluate(LightEffectContext context, EvalIntensityModule[] results)
        {
            for (int i = 0; i < results.Length; i++)
            {
                results[i].Value = BaseValue + i * StepPerSibling;
            }
        }
    }

    [Serializable]
    public sealed class BeatConstantEffect : LightEffectBase<EvalIntensityModule>
    {
        public float Scale = 1f;

        public override void Evaluate(LightEffectContext context, EvalIntensityModule[] results)
        {
            for (int i = 0; i < results.Length; i++)
            {
                results[i].Value = context.ElapsedBeat * Scale;
            }
        }
    }

    public class LightShowEvaluatorTests
    {
        [Test]
        public void AutoDiscovery_FindsMixerStateModulesAndEffects()
        {
            StageTypeRegistry.Invalidate();
            LightTypeCatalog.Invalidate();

            IStageModuleMixer<EvalIntensityModule> mixer = StageTypeRegistry.GetMixer<EvalIntensityModule>();
            Assert.IsInstanceOf<EvalIntensityMixer>(mixer);

            IReadOnlyList<Type> stateModules = LightTypeCatalog.GetModuleTypesForState(typeof(EvalLightState));
            Assert.AreEqual(1, stateModules.Count);
            Assert.AreEqual(typeof(EvalIntensityModule), stateModules[0]);

            IReadOnlyList<Type> effects = LightTypeCatalog.GetEffectTypesForModule(typeof(EvalIntensityModule));
            Assert.IsTrue(Contains(effects, typeof(SiblingRampEffect)));
            Assert.IsTrue(Contains(effects, typeof(BeatConstantEffect)));
        }

        [Test]
        public void StageEffectBase_ReflectionClone_DeepCopiesArraysAndPreservesValues()
        {
            var original = new SiblingRampEffect
            {
                BaseValue = 42f,
                StepPerSibling = 5f,
                Palette = new[] { Color.red, Color.green }
            };

            var cloned = original.Clone() as SiblingRampEffect;
            Assert.IsNotNull(cloned);
            Assert.AreNotSame(original, cloned);
            Assert.AreNotSame(original.Palette, cloned.Palette);
            Assert.AreEqual(42f, cloned.BaseValue, 1e-4f);
            Assert.AreEqual(Color.green, cloned.Palette[1]);

            cloned.Palette[1] = Color.yellow;
            Assert.AreEqual(Color.green, original.Palette[1], "Mutating cloned array must not affect original.");
        }

        [Test]
        public void StageSceneSync_And_StageController_EndToEndLoop()
        {
            var showAsset = ScriptableObject.CreateInstance<StageShowAsset>();
            var timelineAsset = ScriptableObject.CreateInstance<TimelineAsset>();
            StageShow show = showAsset.Show;
            show.TempoMap.Bpm = 120f; // 2 beats per second

            var rootGo = new GameObject("StageRoot");
            var allStageGo = new GameObject("AllStage");
            var leftWingGo = new GameObject("LeftWing");
            var rightWingGo = new GameObject("RightWing");
            var l1Go = new GameObject("L1");
            var l2Go = new GameObject("L2");
            var r1Go = new GameObject("R1");

            allStageGo.transform.SetParent(rootGo.transform);
            leftWingGo.transform.SetParent(allStageGo.transform);
            rightWingGo.transform.SetParent(allStageGo.transform);
            l1Go.transform.SetParent(leftWingGo.transform);
            l2Go.transform.SetParent(leftWingGo.transform);
            r1Go.transform.SetParent(rightWingGo.transform);

            try
            {
                var controller = rootGo.AddComponent<StageController>();
                controller.ShowAsset = showAsset;

                allStageGo.AddComponent<LightGroup>();
                leftWingGo.AddComponent<LightGroup>();
                rightWingGo.AddComponent<LightGroup>();

                var dL1 = new MockEvalLightDriver { CurrentIntensity = 7.5f };
                var dL2 = new MockEvalLightDriver { CurrentIntensity = 8.5f };
                var dR1 = new MockEvalLightDriver { CurrentIntensity = 9.5f };

                var fL1 = l1Go.AddComponent<LightFixture>();
                fL1.Driver = dL1;
                var fL2 = l2Go.AddComponent<LightFixture>();
                fL2.Driver = dL2;
                var fR1 = r1Go.AddComponent<LightFixture>();
                fR1.Driver = dR1;

                // 1. Sync scene hierarchy into StageShow (with individual light wrapping enabled)
                StageSceneSync.SyncTopologyFromScene(controller, wrapIndividualLights: true);
                Assert.AreEqual(1, show.RootNodes.Count);
                var rootGroup = show.RootNodes[0] as LightGroupNode;
                Assert.IsNotNull(rootGroup);
                Assert.AreEqual(2, rootGroup.Children.Count);

                var leftWing = rootGroup.Children[0] as LightGroupNode;
                var rightWing = rootGroup.Children[1] as LightGroupNode;
                Assert.IsNotNull(leftWing);
                Assert.IsNotNull(rightWing);
                Assert.AreEqual(2, leftWing.Children.Count);
                Assert.AreEqual(1, rightWing.Children.Count);

                var r1Wrapper = rightWing.Children[0] as LightGroupNode;
                Assert.IsNotNull(r1Wrapper);
                var r1Leaf = r1Wrapper.Children[0] as LightLeafNode;
                Assert.IsNotNull(r1Leaf);
                Assert.AreSame(fR1, r1Leaf.Fixture);

                // 2. Auto-generate LightTrack<TModule> for all groups from EvalLightState's IHasLightModule<>
                StageSceneSync.EnsureTracksForAllGroups(controller);
                var rootTrack = show.GetTracks(rootGroup)[0] as LightTrack<EvalIntensityModule>;
                var leftTrack = show.GetTracks(leftWing)[0] as LightTrack<EvalIntensityModule>;
                var r1Track = show.GetTracks(r1Wrapper)[0] as LightTrack<EvalIntensityModule>;
                Assert.IsNotNull(rootTrack);
                Assert.IsNotNull(leftTrack);
                Assert.IsNotNull(r1Track);

                // Add clips to generated tracks
                var rootClip = (LightClip<EvalIntensityModule>)rootTrack.CreateClip(0f, 10f);
                rootClip.EaseInDuration = 2f;
                rootClip.EaseOutDuration = 2f;
                rootClip.Effect = new SiblingRampEffect { BaseValue = 10f, StepPerSibling = 10f };

                var leftClip = (LightClip<EvalIntensityModule>)leftTrack.CreateClip(2f, 8f);
                leftClip.BlendMode = StageBlendMode.Additive;
                leftClip.Weight = 1f;
                leftClip.Effect = new SiblingRampEffect { BaseValue = 1f, StepPerSibling = 1f };

                // 3. Auto-build nested Timeline GroupTracks + StageTrackAssets
                StageSceneSync.SyncHierarchyToTimeline(showAsset, timelineAsset);
                int rootGroupTrackCount = 0;
                GroupTrack timelineAllStage = null;
                foreach (TrackAsset t in timelineAsset.GetRootTracks())
                {
                    if (t is GroupTrack gt && gt.name == "AllStage")
                    {
                        rootGroupTrackCount++;
                        timelineAllStage = gt;
                    }
                }

                Assert.AreEqual(1, rootGroupTrackCount);
                Assert.IsNotNull(timelineAllStage);

                int childGroups = 0;
                int childStageTracks = 0;
                foreach (TrackAsset child in timelineAllStage.GetChildTracks())
                {
                    if (child is GroupTrack) childGroups++;
                    if (child is StageTrackAsset) childStageTracks++;
                }

                Assert.AreEqual(2, childGroups, "AllStage GroupTrack should contain LeftWing and RightWing GroupTracks.");
                Assert.AreEqual(1, childStageTracks, "AllStage GroupTrack should contain 1 EvalIntensityModule StageTrackAsset.");

                // 4. Resync after renaming preserves existing node identity and tracks
                allStageGo.name = "AllStage_Renamed";
                StageSceneSync.SyncTopologyFromScene(controller, wrapIndividualLights: true);
                Assert.AreSame(rootGroup, show.RootNodes[0], "Resync must reuse existing LightGroupNode by NodeId.");
                Assert.AreEqual("AllStage_Renamed", rootGroup.Name);
                Assert.AreEqual(1, show.GetTracks(rootGroup).Count, "Existing tracks must survive topology resync.");

                // 5. GatherPreviewProperties -> Preview Evaluate -> ResetPreview cleanup
                var collector = new MockPropertyCollector();
                controller.GatherAllPreviewProperties(collector);
                Assert.IsTrue(collector.CollectedComponents.Contains(l1Go.transform));
                Assert.IsTrue(collector.CollectedComponents.Contains(l2Go.transform));
                Assert.IsTrue(collector.CollectedComponents.Contains(r1Go.transform));

                StageEditorSession.BeginPreview(controller);
                Assert.IsTrue(StageEditorSession.IsPreviewing(controller));

                // t = 1.0s -> root EaseIn (50%)
                controller.EvaluateAt(1f);
                Assert.AreEqual(5f, dL1.CurrentIntensity, 1e-4f);
                Assert.AreEqual(5f, dL2.CurrentIntensity, 1e-4f);
                Assert.AreEqual(10f, dR1.CurrentIntensity, 1e-4f);

                // t = 4.0s -> root Hold + leftTrack Additive
                controller.EvaluateAt(4f);
                Assert.AreEqual(11f, dL1.CurrentIntensity, 1e-4f);
                Assert.AreEqual(12f, dL2.CurrentIntensity, 1e-4f);
                Assert.AreEqual(20f, dR1.CurrentIntensity, 1e-4f);

                // Crossfade + TempoMap on single-light r1Wrapper
                var clipA = (LightClip<EvalIntensityModule>)r1Track.CreateClip(0f, 4f);
                clipA.Effect = new SiblingRampEffect { BaseValue = 100f, StepPerSibling = 0f };
                var clipB = (LightClip<EvalIntensityModule>)r1Track.CreateClip(2f, 6f);
                clipB.Effect = new BeatConstantEffect { Scale = 10f }; // at t=3s -> elapsed 1s = 2 beats -> 20
                controller.EvaluateAt(3f);
                Assert.AreEqual(60f, dR1.CurrentIntensity, 1e-4f);

                // End preview -> invokes ResetPreview on all bound fixture drivers
                StageEditorSession.EndPreview(controller);
                Assert.IsFalse(StageEditorSession.IsPreviewing(controller));
                Assert.IsTrue(dL1.ResetPreviewCalled);
                Assert.IsTrue(dL2.ResetPreviewCalled);
                Assert.IsTrue(dR1.ResetPreviewCalled);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootGo);
                UnityEngine.Object.DestroyImmediate(timelineAsset);
                UnityEngine.Object.DestroyImmediate(showAsset);
            }
        }

        private static bool Contains(IReadOnlyList<Type> list, Type target)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == target)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
