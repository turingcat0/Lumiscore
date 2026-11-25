using System;
using UnityEngine;

namespace Lumiscore.Core
{
    [Serializable]
    public abstract class StageEffectBase
    {
        [SerializeField] private StagePresetManager _presets = new StagePresetManager();

        public StagePresetManager Presets => _presets;
    }
}
