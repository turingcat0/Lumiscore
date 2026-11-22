using System;
using Lumiscore.Core;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine.Timeline;

namespace Lumiscore.Editor.Timeline
{
    public class StageShowEditService
    {
        public StageShowAsset ShowAsset { get; }
        public TimelineAsset TimelineAsset { get; }

        public StageShowEditService(StageShowAsset showAsset, TimelineAsset timelineAsset = null)
        {
            ShowAsset = showAsset;
            TimelineAsset = timelineAsset;
        }

        public void Execute(string undoName, Action<StageShow> editAction)
        {
            if (ShowAsset == null || editAction == null)
            {
                return;
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(undoName);

            Undo.RecordObject(ShowAsset, undoName);
            editAction(ShowAsset.Show);
            EditorUtility.SetDirty(ShowAsset);

            if (TimelineAsset != null)
            {
                StageTimelineSync.SyncShowToTimeline(ShowAsset, TimelineAsset, recordUndo: true, undoName: undoName);
            }

            Undo.CollapseUndoOperations(undoGroup);
            RefreshTimelineView();
        }

        public virtual void RefreshTimelineView()
        {
            if (TimelineEditor.GetWindow() != null)
            {
                TimelineEditor.Refresh(RefreshReason.ContentsModified);
            }

            if (TimelineEditor.inspectedDirector != null)
            {
                TimelineEditor.inspectedDirector.Evaluate();
                SceneView.RepaintAll();
            }
        }
    }
}
