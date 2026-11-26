using System.Collections.Generic;
using Lumiscore.Core;

namespace Lumiscore.Lighting
{
    public interface ILightTrack : IStageTrack
    {
        void EvaluateTrack(
            LightNodeBase targetNode,
            float timelineTime,
            StageTempoMap tempoMap,
            IReadOnlyList<LightNodeBase> children,
            IReadOnlyList<ILightState> prevStates,
            IReadOnlyList<ILightState> workingStates);
    }

    public interface ILightTrack<out TModule> : ILightTrack
        where TModule : class, ILightStateModule
    {
    }
}
