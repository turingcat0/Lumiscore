using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lumiscore.Editor.Presets
{
    [Serializable]
    public sealed class StageEffectPresetPartEntry
    {
        [SerializeField] private string _partKey;
        [SerializeField] private StagePartPresetAsset _partPreset;

        public string PartKey
        {
            get => _partKey;
            set => _partKey = value ?? string.Empty;
        }

        public StagePartPresetAsset PartPreset
        {
            get => _partPreset;
            set => _partPreset = value;
        }

        public StageEffectPresetPartEntry()
        {
        }

        public StageEffectPresetPartEntry(string partKey, StagePartPresetAsset partPreset)
        {
            _partKey = partKey ?? string.Empty;
            _partPreset = partPreset;
        }
    }

    [CreateAssetMenu(menuName = "Lumiscore/Presets/Effect Preset", fileName = "NewEffectPreset")]
    public sealed class StageEffectPresetAsset : ScriptableObject
    {
        [SerializeField] private string _targetEffectTypeName;
        [SerializeField] private List<StageEffectPresetPartEntry> _parts = new List<StageEffectPresetPartEntry>();

        public string TargetEffectTypeName
        {
            get => _targetEffectTypeName;
            set => _targetEffectTypeName = value ?? string.Empty;
        }

        public IReadOnlyList<StageEffectPresetPartEntry> Parts => _parts;

        public Type ResolveTargetEffectType()
            => string.IsNullOrEmpty(_targetEffectTypeName) ? null : Type.GetType(_targetEffectTypeName);

        public StagePartPresetAsset FindPartPreset(string partKey)
        {
            if (string.IsNullOrEmpty(partKey))
            {
                return null;
            }

            for (int i = 0; i < _parts.Count; i++)
            {
                if (_parts[i] != null && _parts[i].PartKey == partKey)
                {
                    return _parts[i].PartPreset;
                }
            }

            return null;
        }

        public void SetPartPreset(string partKey, StagePartPresetAsset partPreset)
        {
            if (string.IsNullOrEmpty(partKey))
            {
                return;
            }

            for (int i = 0; i < _parts.Count; i++)
            {
                if (_parts[i] != null && _parts[i].PartKey == partKey)
                {
                    _parts[i].PartPreset = partPreset;
                    return;
                }
            }

            _parts.Add(new StageEffectPresetPartEntry(partKey, partPreset));
        }

        public bool RemovePartPreset(string partKey)
        {
            if (string.IsNullOrEmpty(partKey))
            {
                return false;
            }

            for (int i = 0; i < _parts.Count; i++)
            {
                if (_parts[i] != null && _parts[i].PartKey == partKey)
                {
                    _parts.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }
    }
}
