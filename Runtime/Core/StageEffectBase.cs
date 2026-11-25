using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;

namespace Lumiscore.Core
{
    [Serializable]
    public abstract class StageEffectBase
    {
        [SerializeField] private StagePresetManager _presets = new StagePresetManager();

        public StagePresetManager Presets => _presets;

        public virtual StageEffectBase Clone()
        {
            var visited = new Dictionary<object, object>(ReferenceEqualityComparer.Instance);
            return (StageEffectBase)DeepCloneObject(this, visited);
        }

        private static object DeepCloneObject(object source, Dictionary<object, object> visited)
        {
            if (source == null)
            {
                return null;
            }

            Type type = source.GetType();
            if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal))
            {
                return source;
            }

            if (source is UnityEngine.Object || type == typeof(Type) || typeof(Delegate).IsAssignableFrom(type))
            {
                return source;
            }

            if (source is AnimationCurve curve)
            {
                return new AnimationCurve(curve.keys)
                {
                    preWrapMode = curve.preWrapMode,
                    postWrapMode = curve.postWrapMode
                };
            }

            if (source is Gradient gradient)
            {
                var copy = new Gradient { mode = gradient.mode };
                copy.SetKeys(gradient.colorKeys, gradient.alphaKeys);
                return copy;
            }

            if (!type.IsValueType && visited.TryGetValue(source, out object existing))
            {
                return existing;
            }

            if (type.IsArray)
            {
                var srcArray = (Array)source;
                Type elemType = type.GetElementType();
                Array dstArray = Array.CreateInstance(elemType, srcArray.Length);
                visited[source] = dstArray;
                for (int i = 0; i < srcArray.Length; i++)
                {
                    dstArray.SetValue(DeepCloneObject(srcArray.GetValue(i), visited), i);
                }

                return dstArray;
            }

            object clone = type.IsValueType
                ? source
                : FormatterServices.GetUninitializedObject(type);

            if (!type.IsValueType)
            {
                visited[source] = clone;
            }

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            for (Type current = type; current != null && current != typeof(object); current = current.BaseType)
            {
                FieldInfo[] fields = current.GetFields(flags);
                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo field = fields[i];
                    if (field.IsNotSerialized)
                    {
                        continue;
                    }

                    object value = field.GetValue(source);
                    field.SetValue(clone, DeepCloneObject(value, visited));
                }
            }

            return clone;
        }

        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();

            public new bool Equals(object x, object y) => ReferenceEquals(x, y);
            public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
