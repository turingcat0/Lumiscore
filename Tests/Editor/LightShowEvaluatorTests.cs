using System;
using System.Collections.Generic;
using Lumiscore.Core;
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

        public ILightState Clone()
        {
            return new EvalLightState
            {
                Intensity = new EvalIntensityModule { Value = Intensity != null ? Intensity.Value : 0f }
            };
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
        public void Evaluator_TopDownSiblingEvaluation_And_EaseInHoldEaseOut_And_Crossfade()
        {
            var show = new StageShow("EvalShow");
            show.TempoMap.Bpm = 120f; // 2 beats per second

            var root = new LightGroupNode("AllStage");
            root.SetStateType(typeof(EvalLightState));

            var leftWing = new LightGroupNode("LeftWing");
            var rightWing = new LightGroupNode("RightWing");
            var l1 = new LightNode("L1");
            var l2 = new LightNode("L2");
            var r1 = new LightNode("R1");

            leftWing.AddChild(l1);
            leftWing.AddChild(l2);
            rightWing.AddChild(r1);
            root.AddChild(leftWing);
            root.AddChild(rightWing);
            show.AddRootNode(root);

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

            var evaluator = new LightShowEvaluator();
            var leafResults = new Dictionary<string, float>();

            // t = 1.0s -> inside root EaseIn (50%), leftTrack inactive
            leafResults.Clear();
            evaluator.Evaluate(show, 1f, (node, state) => leafResults[node.Name] = state.GetModule<EvalIntensityModule>().Value);
            Assert.AreEqual(5f, leafResults["L1"], 1e-4f);
            Assert.AreEqual(5f, leafResults["L2"], 1e-4f);
            Assert.AreEqual(10f, leafResults["R1"], 1e-4f);

            // t = 4.0s -> root Hold (Left=10, Right=20) + leftTrack Additive (L1+=1, L2+=2)
            leafResults.Clear();
            evaluator.Evaluate(show, 4f, (node, state) => leafResults[node.Name] = state.GetModule<EvalIntensityModule>().Value);
            Assert.AreEqual(11f, leafResults["L1"], 1e-4f);
            Assert.AreEqual(12f, leafResults["L2"], 1e-4f);
            Assert.AreEqual(20f, leafResults["R1"], 1e-4f);

            // t = 9.0s -> inside root EaseOut (50% back to 0), leftTrack inactive
            leafResults.Clear();
            evaluator.Evaluate(show, 9f, (node, state) => leafResults[node.Name] = state.GetModule<EvalIntensityModule>().Value);
            Assert.AreEqual(5f, leafResults["L1"], 1e-4f);
            Assert.AreEqual(5f, leafResults["L2"], 1e-4f);
            Assert.AreEqual(10f, leafResults["R1"], 1e-4f);

            // Same-track overlapping Crossfade + TempoMap beats on R1 leaf
            var r1Track = new LightTrack<EvalIntensityModule>(r1);
            var clipA = (LightClip<EvalIntensityModule>)r1Track.CreateClip(0f, 4f);
            clipA.Effect = new SiblingRampEffect { BaseValue = 100f, StepPerSibling = 0f };
            var clipB = (LightClip<EvalIntensityModule>)r1Track.CreateClip(2f, 6f);
            clipB.Effect = new BeatConstantEffect { Scale = 10f }; // at t=3s, elapsed=1s=2 beats => value=20
            show.AddTrack(r1Track);

            // At t = 3.0s: overlap [2, 4] is at 50% => Lerp(100, 20, 0.5) = 60
            leafResults.Clear();
            evaluator.Evaluate(show, 3f, (node, state) => leafResults[node.Name] = state.GetModule<EvalIntensityModule>().Value);
            Assert.AreEqual(60f, leafResults["R1"], 1e-4f);
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
