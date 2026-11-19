using Lumiscore.Core;

namespace Lumiscore.Lighting
{
    public interface ILightTrack<out TModule> : IStageTrack
        where TModule : class, ILightStateModule
    {
        new ILightNode TargetNode { get; }
        void EvaluateTrack(float timelineTime, ILightState prevState);
    }
}
