using Lumiscore.Core;

namespace Lumiscore.Lighting
{
    public class LightClip<TModule> : IStageClip
        where TModule : class, ILightStateModule
    {
        public float StartTime { get; set; }
        public float EndTime { get; set; }
        public ILightEffect<TModule> Effect { get; set; }

        IStageEffect IStageClip.Effect => Effect;

        public float Duration => EndTime - StartTime;

        public bool IsActiveAt(float timelineTime)
            => timelineTime >= StartTime && timelineTime <= EndTime;
    }
}
