using Lumiscore.Patterns;
using Lumiscore.State;

namespace Lumiscore.Tracks
{
    public class PatternClip<TModule>
        where TModule : class, IFixtureStateModule
    {
        public float StartTime { get; set; }
        public float EndTime { get; set; }
        public ILightPattern<TModule> Pattern { get; set; }

        public float Duration => EndTime - StartTime;

        public bool IsActiveAt(float timelineTime)
            => timelineTime >= StartTime && timelineTime <= EndTime;
    }
}
