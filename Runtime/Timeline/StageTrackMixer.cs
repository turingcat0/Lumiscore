using Lumiscore.Core;
using Lumiscore.Lighting;
using UnityEngine.Playables;

namespace Lumiscore.Timeline
{
    public class StageTrackMixer : PlayableBehaviour
    {
        public StageShowAsset ShowAsset { get; set; }
        public string TrackId { get; set; }
        public StageController Controller { get; set; }

        public override void OnGraphStart(Playable playable)
        {
            Controller?.NotifyGraphStart();
        }

        public override void OnGraphStop(Playable playable)
        {
            Controller?.NotifyGraphStop();
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (Controller == null)
            {
                return;
            }

            PlayableGraph graph = playable.GetGraph();
            if (!graph.IsValid() || graph.GetRootPlayableCount() == 0)
            {
                return;
            }

            float timelineTime = (float)graph.GetRootPlayable(0).GetTime();
            Controller.EvaluateFrame(timelineTime, info.frameId);
        }
    }
}
