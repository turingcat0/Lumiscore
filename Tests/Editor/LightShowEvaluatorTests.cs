using System;
using System.Collections.Generic;
using Lumiscore.Core;
using Lumiscore.Editor;
using Lumiscore.Lighting;
using NUnit.Framework;
using UnityEngine;

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

    public sealed class MockEvalLightDriver : LightDriverBase<EvalLightState>
    {
        public float CurrentIntensity = 7.5f;

        public override EvalLightState CaptureState()
        {
            return new EvalLightState
            {
                Intensity = new EvalIntensityModule { Value = CurrentIntensity }
            };
        }

        public override void ApplyState(EvalLightState state)
        {
            if (state?.Intensity != null)
            {
                CurrentIntensity = state.Intensity.Value;
            }
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
        public void StageController_TopDownUniformChildrenEvaluation_PreviewCaptureAndRestore()
        {
            var showAsset = ScriptableObject.CreateInstance<StageShowAsset>();
            StageShow show = showAsset.Show;
            show.TempoMap.Bpm = 120f; // 2 beats per second

            var root = new LightGroupNode("AllStage");
            var leftWing = new LightGroupNode("LeftWing");
            var rightWing = new LightGroupNode("RightWing");

            var l1 = new LightLeafNode("L1");
            var l2 = new LightLeafNode("L2");
            var r1Leaf = new LightLeafNode("R1_Leaf");
            var r1Wrapper = LightGroupNode.WrapSingleLight(r1Leaf, "R1");

            leftWing.AddChild(l1);
            leftWing.AddChild(l2);
            rightWing.AddChild(r1Wrapper);
            root.AddChild(leftWing);
            root.AddChild(rightWing);
            show.AddRootNode(root);

            Assert.AreEqual(0, l1.Index);
            Assert.AreEqual(2, l1.Count);
            Assert.AreEqual(1, l2.Index);
            Assert.AreEqual(2, l2.Count);
            Assert.AreEqual(0, r1Leaf.Index);
            Assert.AreEqual(1, r1Leaf.Count);

            // Root track: sibling 0 (LeftWing) = 10, sibling 1 (RightWing) = 20
            var rootTrack = new LightTrack<EvalIntensityModule>(root);
            var rootClip = (LightClip<EvalIntensityModule>)rootTrack.CreateClip(0f, 10f);
            rootClip.EaseInDuration = 2f;
            rootClip.EaseOutDuration = 2f;
            rootClip.Effect = new SiblingRampEffect { BaseValue = 10f, StepPerSibling = 10f };
            show.AddTrack(rootTrack);

            // LeftWing additive track: sibling 0 (L1) += 1, sibling 1 (L2) += 2
            var leftTrack = new LightTrack<EvalIntensityModule>(leftWing);
            var leftClip = (LightClip<EvalIntensityModule>)leftTrack.CreateClip(2f, 8f);
            leftClip.BlendMode = StageBlendMode.Additive;
            leftClip.Weight = 1f;
            leftClip.Effect = new SiblingRampEffect { BaseValue = 1f, StepPerSibling = 1f };
            show.AddTrack(leftTrack);

            var rootGo = new GameObject("StageRoot");
            var l1Go = new GameObject("L1");
            var l2Go = new GameObject("L2");
            var r1Go = new GameObject("R1");

            try
            {
                var controller = rootGo.AddComponent<StageController>();
                var dL1 = l1Go.AddComponent<MockEvalLightDriver>();
                var dL2 = l2Go.AddComponent<MockEvalLightDriver>();
                var dR1 = r1Go.AddComponent<MockEvalLightDriver>();
                dL1.CurrentIntensity = 7.5f;
                dL2.CurrentIntensity = 8.5f;
                dR1.CurrentIntensity = 9.5f;

                controller.ShowAsset = showAsset;
                controller.BindNode(l1.Id, l1Go.transform, dL1);
                controller.BindNode(l2.Id, l2Go.transform, dL2);
                controller.BindNode(r1Leaf.Id, r1Go.transform, dR1);

                // Begin editor preview session -> captures initial driver states
                StageEditorSession.BeginPreview(controller);
                Assert.IsTrue(StageEditorSession.IsPreviewing(controller));

                // t = 1.0s -> root EaseIn (50%), leftTrack inactive
                controller.EvaluateAt(1f);
                Assert.AreEqual(5f, dL1.CurrentIntensity, 1e-4f);
                Assert.AreEqual(5f, dL2.CurrentIntensity, 1e-4f);
                Assert.AreEqual(10f, dR1.CurrentIntensity, 1e-4f);

                // t = 4.0s -> root Hold + leftTrack Additive
                controller.EvaluateAt(4f);
                Assert.AreEqual(11f, dL1.CurrentIntensity, 1e-4f);
                Assert.AreEqual(12f, dL2.CurrentIntensity, 1e-4f);
                Assert.AreEqual(20f, dR1.CurrentIntensity, 1e-4f);

                // Single-light override track attached to r1Wrapper (Count == 1) with Crossfade + TempoMap
                var r1Track = new LightTrack<EvalIntensityModule>(r1Wrapper);
                var clipA = (LightClip<EvalIntensityModule>)r1Track.CreateClip(0f, 4f);
                clipA.Effect = new SiblingRampEffect { BaseValue = 100f, StepPerSibling = 0f };
                var clipB = (LightClip<EvalIntensityModule>)r1Track.CreateClip(2f, 6f);
                clipB.Effect = new BeatConstantEffect { Scale = 10f }; // at t=3s -> elapsed 1s = 2 beats -> 20
                show.AddTrack(r1Track);

                // t = 3.0s -> overlap [2, 4] at 50% => Lerp(100, 20, 0.5) = 60
                controller.EvaluateAt(3f);
                Assert.AreEqual(60f, dR1.CurrentIntensity, 1e-4f);

                // End editor preview session -> restores initial captured states losslessly
                StageEditorSession.EndPreview(controller);
                Assert.IsFalse(StageEditorSession.IsPreviewing(controller));
                Assert.AreEqual(7.5f, dL1.CurrentIntensity, 1e-4f);
                Assert.AreEqual(8.5f, dL2.CurrentIntensity, 1e-4f);
                Assert.AreEqual(9.5f, dR1.CurrentIntensity, 1e-4f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(r1Go);
                UnityEngine.Object.DestroyImmediate(l2Go);
                UnityEngine.Object.DestroyImmediate(l1Go);
                UnityEngine.Object.DestroyImmediate(rootGo);
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
