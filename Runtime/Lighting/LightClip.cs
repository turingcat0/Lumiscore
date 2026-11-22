using System;
using Lumiscore.Core;
using UnityEngine;

namespace Lumiscore.Lighting
{
    [Serializable]
    public class LightClip<TModule> : IStageClip
        where TModule : class, ILightStateModule
    {
        [SerializeField] private string _id;
        [SerializeField] private float _startTime;
        [SerializeField] private float _endTime;
        [SerializeField] private StageBlendMode _blendMode = StageBlendMode.Override;
        [SerializeField] private float _weight = 1f;
        [SerializeField] private float _easeDuration;
        [SerializeReference] private IStageEffect _effect;

        public string Id => _id;

        public float StartTime
        {
            get => _startTime;
            set => _startTime = value;
        }

        public float EndTime
        {
            get => _endTime;
            set => _endTime = value;
        }

        public StageBlendMode BlendMode
        {
            get => _blendMode;
            set => _blendMode = value;
        }

        public float Weight
        {
            get => _weight;
            set => _weight = value;
        }

        public float EaseDuration
        {
            get => _easeDuration;
            set => _easeDuration = value;
        }

        public ILightEffect<TModule> Effect
        {
            get => _effect as ILightEffect<TModule>;
            set => _effect = value;
        }

        IStageEffect IStageClip.Effect => _effect;

        public bool TrySetEffect(IStageEffect effect)
        {
            if (effect == null)
            {
                _effect = null;
                return true;
            }

            if (effect is ILightEffect<TModule>)
            {
                _effect = effect;
                return true;
            }

            return false;
        }

        public float Duration => _endTime - _startTime;

        public LightClip()
            : this(null)
        {
        }

        public LightClip(string id)
        {
            _id = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString("N") : id;
        }

        public bool IsActiveAt(float timelineTime)
            => timelineTime >= _startTime && timelineTime <= _endTime;
    }
}
