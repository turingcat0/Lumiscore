using System;
using Lumiscore.Core;
using UnityEngine;

namespace Lumiscore.Editor.Presets
{
    [CreateAssetMenu(menuName = "Lumiscore/Presets/Part Preset", fileName = "NewPartPreset")]
    public sealed class StagePartPresetAsset : ScriptableObject
    {
        [SerializeField] private string _targetEffectTypeName;
        [SerializeField] private string _partKey;
        [SerializeField] private StagePresetValueMap _values = new StagePresetValueMap();

        public string TargetEffectTypeName
        {
            get => _targetEffectTypeName;
            set => _targetEffectTypeName = value ?? string.Empty;
        }

        public string PartKey
        {
            get => _partKey;
            set => _partKey = value ?? string.Empty;
        }

        public StagePresetValueMap Values => _values;

        public Type ResolveTargetEffectType()
            => string.IsNullOrEmpty(_targetEffectTypeName) ? null : Type.GetType(_targetEffectTypeName);
    }
}
