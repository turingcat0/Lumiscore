using System.Collections.Generic;

namespace Lumiscore.Lighting
{
    public readonly struct LightEffectContext
    {
        public readonly ILightNode TargetNode;
        public readonly IReadOnlyList<ILightNode> Children;
        public readonly IReadOnlyList<ILightState> PrevStates;
        public readonly float ElapsedTime;
        public readonly float Duration;
        public readonly float ElapsedBeat;
        public readonly float CurrentBpm;

        public LightEffectContext(
            ILightNode targetNode,
            IReadOnlyList<ILightNode> children,
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
