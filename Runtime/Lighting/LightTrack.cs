using System.Collections.Generic;
using Lumiscore.Core;

namespace Lumiscore.Lighting
{
    public class LightTrack<TModule> : ILightTrack<TModule>
        where TModule : class, ILightStateModule
    {
        public ILightNode TargetNode { get; set; }
        IStageNode IStageTrack.TargetNode => TargetNode;

        public List<LightClip<TModule>> Clips { get; } = new List<LightClip<TModule>>();

        public void EvaluateTrack(float timelineTime, ILightState prevState)
        {
        }
    }
}
