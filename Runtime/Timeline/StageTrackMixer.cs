using Lumiscore.Core;
using UnityEngine.Playables;

namespace Lumiscore.Timeline
{
    public class StageTrackMixer : PlayableBehaviour
    {
        public StageShowAsset ShowAsset { get; set; }
        public string TrackId { get; set; }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
        }
    }
}
