using System;
using System.Collections.Generic;
using System.Reflection;

namespace Lumiscore.Core
{
    public sealed class StagePresetPartSchema
    {
        private readonly List<FieldInfo> _fields = new List<FieldInfo>();

        public string Key { get; }
        public string DisplayName { get; }
        public IReadOnlyList<FieldInfo> Fields => _fields;

        public StagePresetPartSchema(string key, string displayName)
        {
            Key = key;
            DisplayName = string.IsNullOrEmpty(displayName) ? key : displayName;
        }

        internal void AddField(FieldInfo field)
        {
            if (field != null)
            {
                _fields.Add(field);
            }
        }

        public bool ContainsField(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                return false;
            }

            for (int i = 0; i < _fields.Count; i++)
            {
                if (_fields[i].Name == fieldName)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public sealed class StagePresetSchema
    {
        private static readonly Dictionary<Type, StagePresetSchema> s_Cache =
            new Dictionary<Type, StagePresetSchema>();

        public Type EffectType { get; }
        public IReadOnlyList<FieldInfo> NonPresetFields { get; }
        public IReadOnlyList<StagePresetPartSchema> Parts { get; }

        private StagePresetSchema(
            Type effectType,
            List<FieldInfo> nonPresetFields,
            List<StagePresetPartSchema> parts)
        {
            EffectType = effectType;
            NonPresetFields = nonPresetFields;
            Parts = parts;
        }

        public static StagePresetSchema For(Type effectType)
        {
            if (effectType == null)
            {
                return null;
            }

            if (s_Cache.TryGetValue(effectType, out StagePresetSchema cached))
            {
                return cached;
            }

            StagePresetSchema built = Build(effectType);
            s_Cache[effectType] = built;
            return built;
        }

        public StagePresetPartSchema FindPart(string partKey)
        {
            if (string.IsNullOrEmpty(partKey))
            {
                return null;
            }

            for (int i = 0; i < Parts.Count; i++)
            {
                if (Parts[i].Key == partKey)
                {
                    return Parts[i];
                }
            }

            return null;
        }

        public StagePresetPartSchema FindPartForField(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                return null;
            }

            for (int i = 0; i < Parts.Count; i++)
            {
                if (Parts[i].ContainsField(fieldName))
                {
                    return Parts[i];
                }
            }

            return null;
        }

        private static StagePresetSchema Build(Type effectType)
        {
            var hierarchy = new List<Type>();
            for (Type current = effectType; current != null && current != typeof(object); current = current.BaseType)
            {
                hierarchy.Add(current);
            }

            hierarchy.Reverse();

            var nonPresetFields = new List<FieldInfo>();
            var parts = new List<StagePresetPartSchema>();
            var partLookup = new Dictionary<string, StagePresetPartSchema>();
            StagePresetPartSchema currentPart = null;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;

            for (int h = 0; h < hierarchy.Count; h++)
            {
                FieldInfo[] declaredFields = hierarchy[h].GetFields(flags);
                Array.Sort(declaredFields, (a, b) => a.MetadataToken.CompareTo(b.MetadataToken));

                for (int i = 0; i < declaredFields.Length; i++)
                {
                    FieldInfo field = declaredFields[i];
                    if (field.IsInitOnly || field.IsNotSerialized)
                    {
                        continue;
                    }

                    var partAttr = field.GetCustomAttribute<StagePresetPartAttribute>(true);
                    if (partAttr != null && !string.IsNullOrEmpty(partAttr.Key))
                    {
                        if (!partLookup.TryGetValue(partAttr.Key, out currentPart))
                        {
                            currentPart = new StagePresetPartSchema(partAttr.Key, partAttr.DisplayName);
                            partLookup[partAttr.Key] = currentPart;
                            parts.Add(currentPart);
                        }
                    }

                    bool ignored = field.IsDefined(typeof(StagePresetIgnoreAttribute), true);
                    if (ignored || currentPart == null)
                    {
                        nonPresetFields.Add(field);
                    }
                    else
                    {
                        currentPart.AddField(field);
                    }
                }
            }

            return new StagePresetSchema(effectType, nonPresetFields, parts);
        }
    }
}
