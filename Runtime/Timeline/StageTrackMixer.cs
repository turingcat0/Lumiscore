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

        private bool _graphStarted;

        public override void OnGraphStart(Playable playable)
        {
            if (!_graphStarted)
            {
                _graphStarted = true;
                Controller?.NotifyGraphStart();
            }
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            NotifyStopOnce();
        }

        public override void OnGraphStop(Playable playable)
        {
            NotifyStopOnce();
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            NotifyStopOnce();
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (Controller == null)
            {
                return;
            }

            if (!_graphStarted)
            {
                _graphStarted = true;
                Controller.NotifyGraphStart();
            }

            PlayableGraph graph = playable.GetGraph();
            if (!graph.IsValid() || graph.GetRootPlayableCount() == 0)
            {
                return;
            }

            float timelineTime = (float)graph.GetRootPlayable(0).GetTime();
            Controller.EvaluateFrame(timelineTime, info.frameId);
        }

        private void NotifyStopOnce()
        {
            if (_graphStarted)
            {
                _graphStarted = false;
                Controller?.NotifyGraphStop();
            }
        }
    }
}
