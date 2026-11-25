using System;
using System.Collections.Generic;
using System.IO;
using Lumiscore.Core;
using Lumiscore.Lighting;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lumiscore.Tests.Editor
{
    public class TestPositionModule : ILightStateModule
    {
        public float Pan;
    }

    [Serializable]
    public class TestPositionEffect : LightEffectBase<TestPositionModule>
    {
        public float Pan = 42.5f;
        public Vector3 Offset = new Vector3(1f, 2f, 3f);
        public Color[] Colors = { Color.red, Color.cyan };
        public AnimationCurve Curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public Gradient Tint = CreateDefaultGradient();

        public override void Evaluate(LightEffectContext context, TestPositionModule[] results)
        {
            for (int i = 0; i < results.Length; i++)
            {
                results[i].Pan = Pan;
            }
        }

        public static Gradient CreateDefaultGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.blue, 0f), new GradientColorKey(Color.magenta, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.5f, 1f) });
            return gradient;
        }
    }

    public interface ITestPresetValue
    {
        object GetBoxed();
    }

    [Serializable]
    public class GenericPresetValue<T> : ITestPresetValue
    {
        [SerializeField] private T _value;

        public T Value
        {
            get => _value;
            set => _value = value;
        }

        public GenericPresetValue()
        {
        }

        public GenericPresetValue(T value)
        {
            _value = value;
        }

        public object GetBoxed() => _value;
    }

    [Serializable]
    public class FloatPresetValue : GenericPresetValue<float>
    {
        public FloatPresetValue() { }
        public FloatPresetValue(float value) : base(value) { }
    }

    [Serializable]
    public class ColorArrayPresetValue : GenericPresetValue<Color[]>
    {
        public ColorArrayPresetValue() { }
        public ColorArrayPresetValue(Color[] value) : base(value) { }
    }

    [Serializable]
    public class CurvePresetValue : GenericPresetValue<AnimationCurve>
    {
        public CurvePresetValue() { }
        public CurvePresetValue(AnimationCurve value) : base(value) { }
    }

    [Serializable]
    public class GradientPresetValue : GenericPresetValue<Gradient>
    {
        public GradientPresetValue() { }
        public GradientPresetValue(Gradient value) : base(value) { }
    }

    [Serializable]
    public class TestNonGenericEffectClip
    {
        [SerializeReference] public StageEffectBase Effect;
    }

    [Serializable]
    public class TestGenericEffectClip<TModule>
        where TModule : class, ILightStateModule
    {
        [SerializeReference] public LightEffectBase<TModule> Effect;
    }

    public class SerializeReferenceVerificationTests
    {
        private const string TempFolder = "Assets/_LumiscoreSerializeTestTemp";

        [OneTimeSetUp]
        public void SetUp()
        {
            if (!AssetDatabase.IsValidFolder(TempFolder))
            {
                AssetDatabase.CreateFolder("Assets", "_LumiscoreSerializeTestTemp");
            }
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder(TempFolder))
            {
                AssetDatabase.DeleteAsset(TempFolder);
                AssetDatabase.Refresh();
            }
        }

        private static T SaveAndReloadFromDisk<T>(T original, string baseName, out string yamlText)
            where T : ScriptableObject
        {
            string pathA = $"{TempFolder}/{baseName}_A.asset";
            string pathB = $"{TempFolder}/{baseName}_B.asset";

            AssetDatabase.CreateAsset(original, pathA);
            AssetDatabase.SaveAssets();

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string fullPathA = Path.Combine(projectRoot, pathA);
            string fullPathB = Path.Combine(projectRoot, pathB);

            yamlText = File.ReadAllText(fullPathA);
            File.Copy(fullPathA, fullPathB, true);
            AssetDatabase.ImportAsset(pathB, ImportAssetOptions.ForceSynchronousImport);

            T reloaded = AssetDatabase.LoadAssetAtPath<T>(pathB);
            Assert.IsNotNull(reloaded, $"Failed to reload {baseName} from disk.");
            Assert.IsFalse(ReferenceEquals(original, reloaded), "Reload returned the same in-memory instance instead of deserializing from disk.");
            return reloaded;
        }

        private static string ExtractManagedReferencesYaml(string yaml)
        {
            int idx = yaml.IndexOf("references:", StringComparison.Ordinal);
            return idx >= 0 ? yaml.Substring(idx) : "(no references: section in YAML)";
        }

        [Test]
        public void Test1_StageShowAsset_CurrentLightTrackAndLightClip_DiskRoundTrip()
        {
            var asset = ScriptableObject.CreateInstance<StageShowAsset>();
            StageShow show = asset.Show;
            show.Name = "TestShow";

            var group = new LightGroupNode("BackRow")
            {
                Layout = new LineLightLayout { Spacing = 2.5f, Direction = Vector3.forward, Centered = false }
            };
            group.AddChild(new LightLeafNode("L1"));
            group.AddChild(new LightLeafNode("L2"));
            show.AddRootNode(group);

            var track = new LightTrack<TestPositionModule>(group);
            var clip = (LightClip<TestPositionModule>)track.CreateClip(1.5f, 6.5f);
            clip.BlendMode = StageBlendMode.Additive;
            clip.Weight = 0.75f;
            clip.EaseInDuration = 0.5f;
            clip.EaseOutDuration = 0.25f;
            clip.Effect = new TestPositionEffect
            {
                Pan = 123.4f,
                Offset = new Vector3(4f, 5f, 6f),
                Colors = new[] { Color.green, Color.yellow }
            };
            show.AddTrack(track);

            StageShowAsset reloaded = SaveAndReloadFromDisk(asset, "Test1_StageShow", out string yaml);
            Debug.Log($"[LumiscoreTest1] TrackCount={reloaded.Show.Tracks.Count}, FirstTrack={reloaded.Show.Tracks[0]?.GetType().FullName ?? "NULL"}\n{ExtractManagedReferencesYaml(yaml)}");

            Assert.AreEqual("TestShow", reloaded.Show.Name);
            Assert.AreEqual(1, reloaded.Show.RootNodes.Count);

            var reloadedGroup = reloaded.Show.RootNodes[0] as LightGroupNode;
            Assert.IsNotNull(reloadedGroup, "LightGroupNode failed to deserialize.");
            Assert.AreEqual(2, reloadedGroup.Children.Count);
            Assert.IsInstanceOf<LineLightLayout>(reloadedGroup.Layout);
            Assert.AreEqual(2.5f, ((LineLightLayout)reloadedGroup.Layout).Spacing, 1e-4f);

            Assert.AreEqual(1, reloaded.Show.Tracks.Count);
            var reloadedTrack = reloaded.Show.Tracks[0] as LightTrack<TestPositionModule>;
            Assert.IsNotNull(reloadedTrack, "LightTrack<TestPositionModule> was dropped or null after disk reload!");
            Assert.IsTrue(ReferenceEquals(reloadedTrack.TargetNode, reloadedGroup), "Shared TargetNode reference was not preserved.");

            Assert.AreEqual(1, reloadedTrack.Clips.Count);
            LightClip<TestPositionModule> reloadedClip = reloadedTrack.Clips[0];
            Assert.AreEqual(1.5f, reloadedClip.StartTime, 1e-4f);
            Assert.AreEqual(6.5f, reloadedClip.EndTime, 1e-4f);
            Assert.AreEqual(StageBlendMode.Additive, reloadedClip.BlendMode);
            Assert.AreEqual(0.75f, reloadedClip.Weight, 1e-4f);
            Assert.AreEqual(0.5f, reloadedClip.EaseInDuration, 1e-4f);
            Assert.AreEqual(0.25f, reloadedClip.EaseOutDuration, 1e-4f);

            var reloadedEffect = reloadedClip.Effect as TestPositionEffect;
            Assert.IsNotNull(reloadedEffect, "LightClip<TModule>._effect (StageEffectBase) was dropped or null after disk reload!");
            Assert.AreEqual(123.4f, reloadedEffect.Pan, 1e-4f);
            Assert.AreEqual(new Vector3(4f, 5f, 6f), reloadedEffect.Offset);
            Assert.AreEqual(2, reloadedEffect.Colors.Length);
            Assert.AreEqual(Color.green, reloadedEffect.Colors[0]);
            Assert.AreEqual(Color.yellow, reloadedEffect.Colors[1]);
            Assert.AreEqual(2, reloadedEffect.Curve.length);
            Assert.AreEqual(2, reloadedEffect.Tint.colorKeys.Length);
        }

        [Test]
        public void Test2_PresetValues_InflatedGenericInstances_DiskRoundTrip()
        {
            var asset = ScriptableObject.CreateInstance<TestPresetContainerAsset>();
            asset.Values.Add(new GenericPresetValue<float>(3.14f));
            asset.Values.Add(new GenericPresetValue<Color[]>(new[] { Color.red, Color.blue }));
            asset.Values.Add(new GenericPresetValue<AnimationCurve>(AnimationCurve.EaseInOut(0f, 0f, 1f, 1f)));
            asset.Values.Add(new GenericPresetValue<Gradient>(TestPositionEffect.CreateDefaultGradient()));

            TestPresetContainerAsset reloaded = SaveAndReloadFromDisk(asset, "Test2_GenericPresetValues", out string yaml);
            Debug.Log($"[LumiscoreTest2] Count={reloaded.Values.Count}, Types=[{string.Join(", ", reloaded.Values.ConvertAll(v => v?.GetType().Name ?? "NULL"))}]\n{ExtractManagedReferencesYaml(yaml)}");

            Assert.AreEqual(4, reloaded.Values.Count);
            Assert.IsInstanceOf<GenericPresetValue<float>>(reloaded.Values[0], "GenericPresetValue<float> failed to deserialize.");
            Assert.AreEqual(3.14f, ((GenericPresetValue<float>)reloaded.Values[0]).Value, 1e-4f);

            Assert.IsInstanceOf<GenericPresetValue<Color[]>>(reloaded.Values[1], "GenericPresetValue<Color[]> failed to deserialize.");
            Assert.AreEqual(2, ((GenericPresetValue<Color[]>)reloaded.Values[1]).Value.Length);
            Assert.AreEqual(Color.blue, ((GenericPresetValue<Color[]>)reloaded.Values[1]).Value[1]);

            Assert.IsInstanceOf<GenericPresetValue<AnimationCurve>>(reloaded.Values[2], "GenericPresetValue<AnimationCurve> failed to deserialize.");
            Assert.AreEqual(2, ((GenericPresetValue<AnimationCurve>)reloaded.Values[2]).Value.length);

            Assert.IsInstanceOf<GenericPresetValue<Gradient>>(reloaded.Values[3], "GenericPresetValue<Gradient> failed to deserialize.");
            Assert.AreEqual(2, ((GenericPresetValue<Gradient>)reloaded.Values[3]).Value.colorKeys.Length);
        }

        [Test]
        public void Test3_PresetValues_NonGenericConcreteSubclasses_DiskRoundTrip()
        {
            var asset = ScriptableObject.CreateInstance<TestPresetContainerAsset>();
            asset.Values.Add(new FloatPresetValue(3.14f));
            asset.Values.Add(new ColorArrayPresetValue(new[] { Color.red, Color.blue }));
            asset.Values.Add(new CurvePresetValue(AnimationCurve.EaseInOut(0f, 0f, 1f, 1f)));
            asset.Values.Add(new GradientPresetValue(TestPositionEffect.CreateDefaultGradient()));

            TestPresetContainerAsset reloaded = SaveAndReloadFromDisk(asset, "Test3_NonGenericPresetValues", out string yaml);
            Debug.Log($"[LumiscoreTest3] Count={reloaded.Values.Count}, Types=[{string.Join(", ", reloaded.Values.ConvertAll(v => v?.GetType().Name ?? "NULL"))}]\n{ExtractManagedReferencesYaml(yaml)}");

            Assert.AreEqual(4, reloaded.Values.Count);
            Assert.IsInstanceOf<FloatPresetValue>(reloaded.Values[0]);
            Assert.AreEqual(3.14f, ((FloatPresetValue)reloaded.Values[0]).Value, 1e-4f);

            Assert.IsInstanceOf<ColorArrayPresetValue>(reloaded.Values[1]);
            Assert.AreEqual(2, ((ColorArrayPresetValue)reloaded.Values[1]).Value.Length);
            Assert.AreEqual(Color.blue, ((ColorArrayPresetValue)reloaded.Values[1]).Value[1]);

            Assert.IsInstanceOf<CurvePresetValue>(reloaded.Values[2]);
            Assert.AreEqual(2, ((CurvePresetValue)reloaded.Values[2]).Value.length);

            Assert.IsInstanceOf<GradientPresetValue>(reloaded.Values[3]);
            Assert.AreEqual(2, ((GradientPresetValue)reloaded.Values[3]).Value.colorKeys.Length);
        }

        [Test]
        public void Test4_EffectField_NonGenericStageEffectBase_Vs_GenericLightEffectBase_DiskRoundTrip()
        {
            var asset = ScriptableObject.CreateInstance<TestClipContainerAsset>();
            asset.NonGenericClip.Effect = new TestPositionEffect { Pan = 77.7f };
            asset.GenericClip.Effect = new TestPositionEffect { Pan = 88.8f };

            TestClipContainerAsset reloaded = SaveAndReloadFromDisk(asset, "Test4_EffectFieldComparison", out string yaml);
            Debug.Log($"[LumiscoreTest4] NonGenericEffect={reloaded.NonGenericClip.Effect?.GetType().Name ?? "NULL"}, GenericEffect={reloaded.GenericClip.Effect?.GetType().Name ?? "NULL"}\n{ExtractManagedReferencesYaml(yaml)}");

            Assert.IsInstanceOf<TestPositionEffect>(reloaded.NonGenericClip.Effect, "Non-generic StageEffectBase failed to deserialize.");
            Assert.AreEqual(77.7f, ((TestPositionEffect)reloaded.NonGenericClip.Effect).Pan, 1e-4f);

            Assert.IsInstanceOf<TestPositionEffect>(reloaded.GenericClip.Effect, "Generic LightEffectBase<TModule> failed to deserialize.");
            Assert.AreEqual(88.8f, ((TestPositionEffect)reloaded.GenericClip.Effect).Pan, 1e-4f);
        }
    }
}
