using System;
using UnityEngine;

namespace Lumiscore.Core
{
    [Serializable]
    public class StageTempoMap
    {
        private const float MinBpm = 0.001f;

        [SerializeField] private float _bpm = 120f;
        [SerializeField] private float _offsetSeconds;

        public float Bpm
        {
            get => _bpm;
            set => _bpm = Mathf.Max(MinBpm, value);
        }

        public float OffsetSeconds
        {
            get => _offsetSeconds;
            set => _offsetSeconds = value;
        }

        public StageTempoMap()
        {
        }

        public StageTempoMap(float bpm, float offsetSeconds = 0f)
        {
            _bpm = Mathf.Max(MinBpm, bpm);
            _offsetSeconds = offsetSeconds;
        }

        public float GetBpmAt(float timeSeconds)
            => Mathf.Max(MinBpm, _bpm);

        public float TimeToBeat(float timeSeconds)
            => (timeSeconds - _offsetSeconds) * (Mathf.Max(MinBpm, _bpm) / 60f);

        public float BeatToTime(float beat)
            => _offsetSeconds + beat * (60f / Mathf.Max(MinBpm, _bpm));
    }
}
