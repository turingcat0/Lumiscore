using System.Collections.Generic;

namespace Lumiscore.Lighting
{
    public readonly struct LightEffectContext
    {
        public readonly LightNodeBase TargetNode;
        public readonly IReadOnlyList<LightNodeBase> Children;
        public readonly IReadOnlyList<ILightState> PrevStates;
        public readonly float ElapsedTime;
        public readonly float Duration;
        public readonly float ElapsedBeat;
        public readonly float CurrentBpm;

        public LightEffectContext(
            LightNodeBase targetNode,
            IReadOnlyList<LightNodeBase> children,
            IReadOnlyList<ILightState> prevStates,
            float elapsedTime,
            float duration,
            float elapsedBeat,
            float currentBpm)
        {
            TargetNode = targetNode;
            Children = children;
            PrevStates = prevStates;
            ElapsedTime = elapsedTime;
            Duration = duration;
            ElapsedBeat = elapsedBeat;
            CurrentBpm = currentBpm;
        }
    }
}
