using System;
using UnityEngine;

namespace Lumiscore.Core
{
    [Serializable]
    public sealed class FloatPresetValue : StagePresetValueBase<float>
    {
        public FloatPresetValue() { }
        public FloatPresetValue(float value) : base(value) { }
        public override IStagePresetValue Clone() => new FloatPresetValue(Value);
    }

    [Serializable]
    public sealed class IntPresetValue : StagePresetValueBase<int>
    {
        public IntPresetValue() { }
        public IntPresetValue(int value) : base(value) { }
        public override IStagePresetValue Clone() => new IntPresetValue(Value);
    }

    [Serializable]
    public sealed class BoolPresetValue : StagePresetValueBase<bool>
    {
        public BoolPresetValue() { }
        public BoolPresetValue(bool value) : base(value) { }
        public override IStagePresetValue Clone() => new BoolPresetValue(Value);
    }

    [Serializable]
    public sealed class StringPresetValue : StagePresetValueBase<string>
    {
        public StringPresetValue() { }
        public StringPresetValue(string value) : base(value) { }
        public override IStagePresetValue Clone() => new StringPresetValue(Value);
    }

    [Serializable]
    public sealed class Vector2PresetValue : StagePresetValueBase<Vector2>
    {
        public Vector2PresetValue() { }
        public Vector2PresetValue(Vector2 value) : base(value) { }
        public override IStagePresetValue Clone() => new Vector2PresetValue(Value);
    }

    [Serializable]
    public sealed class Vector3PresetValue : StagePresetValueBase<Vector3>
    {
        public Vector3PresetValue() { }
        public Vector3PresetValue(Vector3 value) : base(value) { }
        public override IStagePresetValue Clone() => new Vector3PresetValue(Value);
    }

    [Serializable]
    public sealed class Vector4PresetValue : StagePresetValueBase<Vector4>
    {
        public Vector4PresetValue() { }
        public Vector4PresetValue(Vector4 value) : base(value) { }
        public override IStagePresetValue Clone() => new Vector4PresetValue(Value);
    }

    [Serializable]
    public sealed class ColorPresetValue : StagePresetValueBase<Color>
    {
        public ColorPresetValue() { }
        public ColorPresetValue(Color value) : base(value) { }
        public override IStagePresetValue Clone() => new ColorPresetValue(Value);
    }

    [Serializable]
    public sealed class ColorArrayPresetValue : StagePresetValueBase<Color[]>
    {
        public ColorArrayPresetValue() { }
        public ColorArrayPresetValue(Color[] value) : base(CloneColors(value)) { }

        public override IStagePresetValue Clone() => new ColorArrayPresetValue(Value);

        protected override Color[] CloneValue(Color[] source) => CloneColors(source);

        private static Color[] CloneColors(Color[] source)
        {
            if (source == null)
            {
                return null;
            }

            var copy = new Color[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }
    }

    [Serializable]
    public sealed class AnimationCurvePresetValue : StagePresetValueBase<AnimationCurve>
    {
        public AnimationCurvePresetValue() { }
        public AnimationCurvePresetValue(AnimationCurve value) : base(CloneCurve(value)) { }

        public override IStagePresetValue Clone() => new AnimationCurvePresetValue(Value);

        protected override AnimationCurve CloneValue(AnimationCurve source) => CloneCurve(source);

        private static AnimationCurve CloneCurve(AnimationCurve source)
        {
            if (source == null)
            {
                return null;
            }

            return new AnimationCurve(source.keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
        }
    }

    [Serializable]
    public sealed class GradientPresetValue : StagePresetValueBase<Gradient>
    {
        public GradientPresetValue() { }
        public GradientPresetValue(Gradient value) : base(CloneGradient(value)) { }

        public override IStagePresetValue Clone() => new GradientPresetValue(Value);

        protected override Gradient CloneValue(Gradient source) => CloneGradient(source);

        private static Gradient CloneGradient(Gradient source)
        {
            if (source == null)
            {
                return null;
            }

            var copy = new Gradient { mode = source.mode };
            copy.SetKeys(source.colorKeys, source.alphaKeys);
            return copy;
        }
    }

    [Serializable]
    public sealed class ObjectPresetValue : StagePresetValueBase<UnityEngine.Object>
    {
        public ObjectPresetValue() { }
        public ObjectPresetValue(UnityEngine.Object value) : base(value) { }
        public override IStagePresetValue Clone() => new ObjectPresetValue(Value);
    }

    [Serializable]
    public sealed class ReferencePresetValue : IStagePresetValue
    {
        [SerializeReference] private object _value;

        public Type ValueType => _value?.GetType() ?? typeof(object);

        public ReferencePresetValue() { }

        public ReferencePresetValue(object value)
        {
            _value = value;
        }

        public object GetBoxedValue() => _value;

        public void SetBoxedValue(object value)
        {
            _value = value;
        }

        public IStagePresetValue Clone() => new ReferencePresetValue(_value);
    }

    public static class StagePresetValueFactory
    {
        public static IStagePresetValue Create(Type valueType, object value)
        {
            if (valueType == null)
            {
                return null;
            }

            if (valueType == typeof(float)) return new FloatPresetValue(value is float f ? f : default);
            if (valueType == typeof(int)) return new IntPresetValue(value is int i ? i : default);
            if (valueType.IsEnum) return new IntPresetValue(value != null ? Convert.ToInt32(value) : 0);
            if (valueType == typeof(bool)) return new BoolPresetValue(value is bool b ? b : default);
            if (valueType == typeof(string)) return new StringPresetValue(value as string);
            if (valueType == typeof(Vector2)) return new Vector2PresetValue(value is Vector2 v2 ? v2 : default);
            if (valueType == typeof(Vector3)) return new Vector3PresetValue(value is Vector3 v3 ? v3 : default);
            if (valueType == typeof(Vector4)) return new Vector4PresetValue(value is Vector4 v4 ? v4 : default);
            if (valueType == typeof(Color)) return new ColorPresetValue(value is Color c ? c : default);
            if (valueType == typeof(Color[])) return new ColorArrayPresetValue(value as Color[]);
            if (valueType == typeof(AnimationCurve)) return new AnimationCurvePresetValue(value as AnimationCurve);
            if (valueType == typeof(Gradient)) return new GradientPresetValue(value as Gradient);
            if (typeof(UnityEngine.Object).IsAssignableFrom(valueType)) return new ObjectPresetValue(value as UnityEngine.Object);

            if (!valueType.IsValueType && !valueType.IsGenericType)
            {
                return new ReferencePresetValue(value);
            }

            return null;
        }
    }
}
