using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Lumiscore.Core
{
    [Serializable]
    public sealed class StagePartPresetBinding
    {
        [SerializeField] private string _partKey;
        [SerializeField] private string _partPresetId;
        [SerializeField] private bool _referenceOverride;
        [SerializeField] private List<string> _overriddenFields = new List<string>();

        public string PartKey => _partKey;

        public string PartPresetId
        {
            get => _partPresetId;
            set => _partPresetId = value ?? string.Empty;
        }

        public bool ReferenceOverride
        {
            get => _referenceOverride;
            set => _referenceOverride = value;
        }

        public IReadOnlyList<string> OverriddenFields => _overriddenFields;

        public StagePartPresetBinding()
        {
        }

        public StagePartPresetBinding(string partKey)
        {
            _partKey = partKey ?? string.Empty;
        }

        public bool IsFieldOverridden(string fieldName)
            => !string.IsNullOrEmpty(fieldName) && _overriddenFields.Contains(fieldName);

        public void MarkFieldOverridden(string fieldName)
        {
            if (!string.IsNullOrEmpty(fieldName) && !_overriddenFields.Contains(fieldName))
            {
                _overriddenFields.Add(fieldName);
            }
        }

        public bool ClearFieldOverride(string fieldName)
            => !string.IsNullOrEmpty(fieldName) && _overriddenFields.Remove(fieldName);

        public void ClearAllOverrides()
        {
            _overriddenFields.Clear();
        }
    }

    [Serializable]
    public sealed class StagePresetManager
    {
        [SerializeField] private string _effectPresetId;
        [SerializeField] private List<StagePartPresetBinding> _partBindings = new List<StagePartPresetBinding>();

        [NonSerialized] private StagePresetSchema _schema;

        public string EffectPresetId
        {
            get => _effectPresetId;
            set => _effectPresetId = value ?? string.Empty;
        }

        public IReadOnlyList<StagePartPresetBinding> PartBindings => _partBindings;
        public StagePresetSchema Schema => _schema;

        public void InjectSchema(StagePresetSchema schema)
        {
            _schema = schema;
        }

        public StagePartPresetBinding FindPartBinding(string partKey)
        {
            if (string.IsNullOrEmpty(partKey))
            {
                return null;
            }

            for (int i = 0; i < _partBindings.Count; i++)
            {
                if (_partBindings[i] != null && _partBindings[i].PartKey == partKey)
                {
                    return _partBindings[i];
                }
            }

            return null;
        }

        public StagePartPresetBinding GetOrCreatePartBinding(string partKey)
        {
            StagePartPresetBinding existing = FindPartBinding(partKey);
            if (existing != null)
            {
                return existing;
            }

            var created = new StagePartPresetBinding(partKey);
            _partBindings.Add(created);
            return created;
        }

        public void SelectEffectPreset(string effectPresetId)
        {
            _effectPresetId = effectPresetId ?? string.Empty;
            if (string.IsNullOrEmpty(_effectPresetId))
            {
                for (int i = 0; i < _partBindings.Count; i++)
                {
                    if (_partBindings[i] != null)
                    {
                        _partBindings[i].ReferenceOverride = false;
                    }
                }
            }
        }

        public void SelectPartPreset(string partKey, string partPresetId, bool referenceOverride = false)
        {
            if (string.IsNullOrEmpty(partKey))
            {
                return;
            }

            StagePartPresetBinding binding = GetOrCreatePartBinding(partKey);
            binding.PartPresetId = partPresetId;
            binding.ReferenceOverride = referenceOverride;
            binding.ClearAllOverrides();
        }

        public bool IsFieldOverridden(string partKey, string fieldName)
        {
            StagePartPresetBinding binding = FindPartBinding(partKey);
            return binding != null && binding.IsFieldOverridden(fieldName);
        }

        public void SetFieldOverridden(string partKey, string fieldName, bool overridden)
        {
            if (string.IsNullOrEmpty(partKey) || string.IsNullOrEmpty(fieldName))
            {
                return;
            }

            StagePartPresetBinding binding = GetOrCreatePartBinding(partKey);
            if (overridden)
            {
                binding.MarkFieldOverridden(fieldName);
            }
            else
            {
                binding.ClearFieldOverride(fieldName);
            }
        }

        public void ClearOverrides(string partKey)
        {
            FindPartBinding(partKey)?.ClearAllOverrides();
        }

        public void ApplyPartValues(
            StageEffectBase targetEffect,
            string partKey,
            StagePresetValueMap values,
            bool respectOverrides = true)
        {
            if (targetEffect == null || string.IsNullOrEmpty(partKey) || values == null)
            {
                return;
            }

            StagePresetSchema schema = _schema ?? StagePresetSchema.For(targetEffect.GetType());
            StagePresetPartSchema partSchema = schema?.FindPart(partKey);
            if (partSchema == null)
            {
                return;
            }

            StagePartPresetBinding binding = FindPartBinding(partKey);
            IReadOnlyList<FieldInfo> fields = partSchema.Fields;

            for (int i = 0; i < fields.Count; i++)
            {
                FieldInfo field = fields[i];
                if (respectOverrides && binding != null && binding.IsFieldOverridden(field.Name))
                {
                    continue;
                }

                if (!values.TryGetValue(field.Name, out IStagePresetValue presetValue) || presetValue == null)
                {
                    continue;
                }

                object raw = presetValue.GetBoxedValue();
                if (field.FieldType.IsEnum && raw is int intVal)
                {
                    field.SetValue(targetEffect, Enum.ToObject(field.FieldType, intVal));
                }
                else if (raw == null || field.FieldType.IsInstanceOfType(raw))
                {
                    field.SetValue(targetEffect, raw);
                }
            }
        }
    }
}
