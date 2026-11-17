using System.Collections.Generic;
using Lumiscore.Nodes;
using Lumiscore.State;

namespace Lumiscore.Tracks
{
    public class LightTrack<TModule> : ILightTrack<TModule>
        where TModule : class, IFixtureStateModule
    {
        public ILightNode TargetNode { get; set; }
        public List<PatternClip<TModule>> Clips { get; } = new List<PatternClip<TModule>>();

        public void EvaluateTrack(float timelineTime, IFixtureState prevState)
        {
        }
    }
}
