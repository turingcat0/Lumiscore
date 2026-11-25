using System.Collections.Generic;
using Lumiscore.Core;

namespace Lumiscore.Lighting
{
    public interface ILightTrack : IStageTrack
    {
        new ILightNode TargetNode { get; }

        void EvaluateTrack(
            float timelineTime,
            StageTempoMap tempoMap,
            IReadOnlyList<ILightNode> children,
            IReadOnlyList<ILightState> prevStates,
            IReadOnlyList<ILightState> workingStates);
    }

    public interface ILightTrack<out TModule> : ILightTrack
        where TModule : class, ILightStateModule
    {
    }
}
