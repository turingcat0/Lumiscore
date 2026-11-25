using System;
using System.IO;
using Lumiscore.Core;
using Lumiscore.Editor.Presets;
using Lumiscore.Lighting;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lumiscore.Tests.Editor
{
    public class PresetTestModule : ILightStateModule
    {
        public float Intensity;
    }

    [Serializable]
    public abstract class PresetTestBaseEffect : LightEffectBase<PresetTestModule>
    {
        public float UnassignedHeaderField = 1f;

        [StagePresetPart("timing", "Timing")]
        public float Speed = 2f;
        public float Phase = 0.5f;

        [StagePresetIgnore]
        public string EditorNote = "note";

        [SerializeField] private float _hiddenPrivate = 99f;
        public float HiddenPrivate => _hiddenPrivate;
    }

    [Serializable]
    public class PresetTestDerivedEffect : PresetTestBaseEffect
    {
        [StagePresetPart("color", "Color")]
        public Color PrimaryColor = Color.white;
        public Color[] Palette = { Color.red, Color.blue };
        public AnimationCurve Curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public Gradient Gradient = CreateGradient();

        [StagePresetPart("timing", "Timing")]
        public float LoopCount = 3f;

        public override void Evaluate(LightEffectContext context, PresetTestModule[] results)
        {
            for (int i = 0; i < results.Length; i++)
            {
                results[i].Intensity = Speed;
            }
        }

        public static Gradient CreateGradient()
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.cyan, 0f), new GradientColorKey(Color.yellow, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.25f, 1f) });
            return g;
        }
    }

    public class StagePresetFrameworkTests
    {
        private const string TempFolder = "Assets/_LumiscorePresetTestTemp";

        [OneTimeSetUp]
        public void SetUp()
        {
            if (!AssetDatabase.IsValidFolder(TempFolder))
            {
                AssetDatabase.CreateFolder("Assets", "_LumiscorePresetTestTemp");
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

        [Test]
        public void Schema_ScansStreamingParts_MergesSameKey_AndRespectsIgnore()
        {
            StagePresetSchema schema = StagePresetSchema.For(typeof(PresetTestDerivedEffect));
            Assert.IsNotNull(schema);

            // Non-preset fields: UnassignedHeaderField (before first anchor) and EditorNote ([StagePresetIgnore])
            Assert.AreEqual(2, schema.NonPresetFields.Count);
            Assert.AreEqual(nameof(PresetTestBaseEffect.UnassignedHeaderField), schema.NonPresetFields[0].Name);
            Assert.AreEqual(nameof(PresetTestBaseEffect.EditorNote), schema.NonPresetFields[1].Name);

            // Two parts: "timing" (Speed, Phase, LoopCount merged) and "color" (PrimaryColor, Palette, Curve, Gradient)
            Assert.AreEqual(2, schema.Parts.Count);

            StagePresetPartSchema timing = schema.FindPart("timing");
            Assert.IsNotNull(timing);
            Assert.AreEqual(3, timing.Fields.Count);
            Assert.AreEqual(nameof(PresetTestBaseEffect.Speed), timing.Fields[0].Name);
            Assert.AreEqual(nameof(PresetTestBaseEffect.Phase), timing.Fields[1].Name);
            Assert.AreEqual(nameof(PresetTestDerivedEffect.LoopCount), timing.Fields[2].Name);

            StagePresetPartSchema color = schema.FindPart("color");
            Assert.IsNotNull(color);
            Assert.AreEqual(4, color.Fields.Count);
            Assert.IsTrue(color.ContainsField(nameof(PresetTestDerivedEffect.PrimaryColor)));
            Assert.IsTrue(color.ContainsField(nameof(PresetTestDerivedEffect.Palette)));
            Assert.IsTrue(color.ContainsField(nameof(PresetTestDerivedEffect.Curve)));
            Assert.IsTrue(color.ContainsField(nameof(PresetTestDerivedEffect.Gradient)));

            // Private fields must not be included
            Assert.IsNull(schema.FindPartForField("_hiddenPrivate"));
        }

        [Test]
        public void PresetManager_AppliesValues_AndRespectsFieldOverrides()
        {
            var effect = new PresetTestDerivedEffect
            {
                Speed = 1f,
                Phase = 0f,
                LoopCount = 1f
            };

            effect.Presets.SetFieldOverridden("timing", nameof(PresetTestBaseEffect.Phase), true);
            Assert.IsTrue(effect.Presets.IsFieldOverridden("timing", nameof(PresetTestBaseEffect.Phase)));

            var map = new StagePresetValueMap();
            map.SetValue(nameof(PresetTestBaseEffect.Speed), StagePresetValueFactory.Create(typeof(float), 8.5f));
            map.SetValue(nameof(PresetTestBaseEffect.Phase), StagePresetValueFactory.Create(typeof(float), 0.9f));
            map.SetValue(nameof(PresetTestDerivedEffect.LoopCount), StagePresetValueFactory.Create(typeof(float), 5f));

            effect.Presets.ApplyPartValues(effect, "timing", map, respectOverrides: true);

            Assert.AreEqual(8.5f, effect.Speed, 1e-4f);
            Assert.AreEqual(0f, effect.Phase, 1e-4f, "Overridden field should not be overwritten.");
            Assert.AreEqual(5f, effect.LoopCount, 1e-4f);

            effect.Presets.ClearOverrides("timing");
            effect.Presets.ApplyPartValues(effect, "timing", map, respectOverrides: true);
            Assert.AreEqual(0.9f, effect.Phase, 1e-4f, "Cleared override should now accept preset value.");
        }

        [Test]
        public void PartAndEffectPresetAssets_DiskRoundTrip_SupportsSharedPartReferences()
        {
            string partPath = $"{TempFolder}/SharedColorPart.asset";
            string effectAPath = $"{TempFolder}/EffectPresetA.asset";
            string effectBPath = $"{TempFolder}/EffectPresetB.asset";

            var partAsset = ScriptableObject.CreateInstance<StagePartPresetAsset>();
            partAsset.TargetEffectTypeName = typeof(PresetTestDerivedEffect).AssemblyQualifiedName;
            partAsset.PartKey = "color";
            partAsset.Values.SetValue(nameof(PresetTestDerivedEffect.PrimaryColor), StagePresetValueFactory.Create(typeof(Color), Color.magenta));
            partAsset.Values.SetValue(nameof(PresetTestDerivedEffect.Palette), StagePresetValueFactory.Create(typeof(Color[]), new[] { Color.green, Color.yellow }));
            partAsset.Values.SetValue(nameof(PresetTestDerivedEffect.Curve), StagePresetValueFactory.Create(typeof(AnimationCurve), AnimationCurve.Linear(0f, 0f, 1f, 1f)));
            partAsset.Values.SetValue(nameof(PresetTestDerivedEffect.Gradient), StagePresetValueFactory.Create(typeof(Gradient), PresetTestDerivedEffect.CreateGradient()));

            AssetDatabase.CreateAsset(partAsset, partPath);

            var effectA = ScriptableObject.CreateInstance<StageEffectPresetAsset>();
            effectA.TargetEffectTypeName = typeof(PresetTestDerivedEffect).AssemblyQualifiedName;
            effectA.SetPartPreset("color", partAsset);
            AssetDatabase.CreateAsset(effectA, effectAPath);

            var effectB = ScriptableObject.CreateInstance<StageEffectPresetAsset>();
            effectB.TargetEffectTypeName = typeof(PresetTestDerivedEffect).AssemblyQualifiedName;
            effectB.SetPartPreset("color", partAsset);
            AssetDatabase.CreateAsset(effectB, effectBPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Copy to fresh paths to force true YAML deserialization from disk
            string reloadedPartPath = $"{TempFolder}/SharedColorPart_Reloaded.asset";
            File.Copy(partPath, reloadedPartPath, true);
            AssetDatabase.ImportAsset(reloadedPartPath, ImportAssetOptions.ForceUpdate);

            var reloadedPart = AssetDatabase.LoadAssetAtPath<StagePartPresetAsset>(reloadedPartPath);
            Assert.IsNotNull(reloadedPart);
            Assert.AreEqual(typeof(PresetTestDerivedEffect), reloadedPart.ResolveTargetEffectType());
            Assert.AreEqual("color", reloadedPart.PartKey);
            Assert.AreEqual(4, reloadedPart.Values.Count);

            Assert.IsTrue(reloadedPart.Values.TryGetValue(nameof(PresetTestDerivedEffect.PrimaryColor), out IStagePresetValue colorVal));
            Assert.AreEqual(Color.magenta, (Color)colorVal.GetBoxedValue());

            Assert.IsTrue(reloadedPart.Values.TryGetValue(nameof(PresetTestDerivedEffect.Palette), out IStagePresetValue paletteVal));
            var palette = (Color[])paletteVal.GetBoxedValue();
            Assert.AreEqual(2, palette.Length);
            Assert.AreEqual(Color.yellow, palette[1]);

            Assert.IsTrue(reloadedPart.Values.TryGetValue(nameof(PresetTestDerivedEffect.Curve), out IStagePresetValue curveVal));
            Assert.AreEqual(2, ((AnimationCurve)curveVal.GetBoxedValue()).length);

            Assert.IsTrue(reloadedPart.Values.TryGetValue(nameof(PresetTestDerivedEffect.Gradient), out IStagePresetValue gradVal));
            Assert.AreEqual(2, ((Gradient)gradVal.GetBoxedValue()).colorKeys.Length);

            var reloadedA = AssetDatabase.LoadAssetAtPath<StageEffectPresetAsset>(effectAPath);
            var reloadedB = AssetDatabase.LoadAssetAtPath<StageEffectPresetAsset>(effectBPath);
            Assert.IsTrue(ReferenceEquals(reloadedA.FindPartPreset("color"), reloadedB.FindPartPreset("color")),
                "Multiple effect presets must be able to share the same part preset asset.");
        }
    }
}
