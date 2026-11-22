using Lumiscore.Timeline;
using UnityEditor.Timeline;
using UnityEngine.Timeline;

namespace Lumiscore.Editor.Timeline
{
    [CustomTimelineEditor(typeof(StageClipAsset))]
    public class StageClipEditor : ClipEditor
    {
        public override ClipDrawOptions GetClipOptions(TimelineClip clip)
        {
            ClipDrawOptions options = base.GetClipOptions(clip);
            string validationError = StageTimelineSync.GetClipValidationError(clip);
            if (!string.IsNullOrEmpty(validationError))
            {
                options.errorText = validationError;
            }

            return options;
        }

        public override void DrawBackground(TimelineClip clip, ClipBackgroundRegion region)
        {
            base.DrawBackground(clip, region);
        }

        public override void OnCreate(TimelineClip clip, TrackAsset track, TimelineClip clonedFrom)
        {
            base.OnCreate(clip, track, clonedFrom);
            StageTimelineSync.HandleClipCreated(clip, track, clonedFrom);
        }

        public override void OnClipChanged(TimelineClip clip)
        {
            base.OnClipChanged(clip);
            StageTimelineSync.HandleClipChanged(clip);
        }
    }
}
