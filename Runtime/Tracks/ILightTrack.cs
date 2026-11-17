using Lumiscore.Nodes;
using Lumiscore.State;

namespace Lumiscore.Tracks
{
    public interface ILightTrack<out TModule>
        where TModule : class, IFixtureStateModule
    {
        ILightNode TargetNode { get; }
        void EvaluateTrack(float timelineTime, IFixtureState prevState);
    }
}
