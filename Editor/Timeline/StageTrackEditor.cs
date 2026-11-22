using Lumiscore.Timeline;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace Lumiscore.Editor.Timeline
{
    [CustomTimelineEditor(typeof(StageTrackAsset))]
    public class StageTrackEditor : TrackEditor
    {
        public override TrackDrawOptions GetTrackOptions(TrackAsset track, Object binding)
        {
            return base.GetTrackOptions(track, binding);
        }

        public override void OnCreate(TrackAsset track, TrackAsset copiedFrom)
        {
            base.OnCreate(track, copiedFrom);
        }

        public override void OnTrackChanged(TrackAsset track)
        {
            base.OnTrackChanged(track);
            StageTimelineSync.HandleTrackChanged(track);
        }
    }
}
