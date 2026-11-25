using System.Collections.Generic;
using Lumiscore.Core;
using Lumiscore.Timeline;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace Lumiscore.Editor.Timeline
{
    [InitializeOnLoad]
    public static class StageTimelineSync
    {
        private const float TimeEpsilon = 1e-4f;
        private const float MinClipDuration = 0.001f;

        private static readonly Dictionary<string, StageClipAsset> s_LiveClipAssets =
            new Dictionary<string, StageClipAsset>();

        public static bool IsSyncing { get; private set; }

        static StageTimelineSync()
        {
            Undo.undoRedoPerformed -= HandleUndoRedoPerformed;
            Undo.undoRedoPerformed += HandleUndoRedoPerformed;
        }

        public static void HandleClipCreated(TimelineClip clip, TrackAsset track, TimelineClip clonedFrom)
        {
            if (IsSyncing || clip == null || !(track is StageTrackAsset trackAsset) || !(clip.asset is StageClipAsset clipAsset))
            {
                return;
            }

            StageShowAsset showAsset = trackAsset.ShowAsset;
            if (showAsset == null || showAsset.Show == null)
            {
                return;
            }

            IStageTrack modelTrack = showAsset.Show.FindTrack(trackAsset.TrackId);
            if (modelTrack == null)
            {
                return;
            }

            if (clonedFrom != null)
            {
                string sourceClipId = (clonedFrom.asset as StageClipAsset)?.ClipId;
                IStageClip sourceModelClip = showAsset.Show.FindClip(sourceClipId);

                Undo.RecordObject(showAsset, "Duplicate Stage Clip");
                Undo.RecordObject(clipAsset, "Duplicate Stage Clip");

                IStageClip clonedModelClip = sourceModelClip != null
                    ? modelTrack.CloneClip(sourceModelClip, (float)clip.start, (float)clip.end)
                    : modelTrack.CreateClip((float)clip.start, (float)clip.end);

                if (clonedModelClip == null)
                {
                    return;
                }

                clipAsset.ClipId = clonedModelClip.Id;
                RegisterLiveClipAsset(clipAsset);
                EditorUtility.SetDirty(clipAsset);
                EditorUtility.SetDirty(showAsset);
                return;
            }

            if (!string.IsNullOrEmpty(clipAsset.ClipId) && modelTrack.FindClip(clipAsset.ClipId) != null)
            {
                RegisterLiveClipAsset(clipAsset);
                return;
            }

            Undo.RecordObject(showAsset, "Create Stage Clip");
            Undo.RecordObject(clipAsset, "Create Stage Clip");

            IStageClip newModelClip = modelTrack.CreateClip((float)clip.start, (float)clip.end);
            clipAsset.ClipId = newModelClip.Id;
            RegisterLiveClipAsset(clipAsset);
            EditorUtility.SetDirty(clipAsset);
            EditorUtility.SetDirty(showAsset);
        }

        public static void HandleClipChanged(TimelineClip clip)
        {
            if (IsSyncing || clip == null || !(clip.GetParentTrack() is StageTrackAsset trackAsset) || !(clip.asset is StageClipAsset clipAsset))
            {
                return;
            }

            RegisterLiveClipAsset(clipAsset);

            StageShowAsset showAsset = trackAsset.ShowAsset;
            if (showAsset == null || showAsset.Show == null || string.IsNullOrEmpty(clipAsset.ClipId))
            {
                return;
            }

            IStageTrack targetTrack = showAsset.Show.FindTrack(trackAsset.TrackId);
            if (targetTrack == null)
            {
                return;
            }

            IStageClip modelClip = targetTrack.FindClip(clipAsset.ClipId);
            if (modelClip == null)
            {
                IStageTrack sourceTrack = showAsset.Show.FindTrackContainingClip(clipAsset.ClipId);
                if (sourceTrack == null || sourceTrack == targetTrack)
                {
                    return;
                }

                IStageClip movingClip = sourceTrack.FindClip(clipAsset.ClipId);
                if (movingClip == null || !targetTrack.CanAcceptClip(movingClip))
                {
                    return;
                }

                Undo.RecordObject(showAsset, "Move Stage Clip Between Tracks");
                sourceTrack.RemoveClip(clipAsset.ClipId);
                targetTrack.AddClip(movingClip);
                modelClip = movingClip;
                EditorUtility.SetDirty(showAsset);
            }

            float newStart = (float)clip.start;
            float newEnd = (float)clip.end;
            float newEaseIn = (float)clip.easeInDuration;
            float newEaseOut = (float)clip.easeOutDuration;

            if (Mathf.Abs(modelClip.StartTime - newStart) <= TimeEpsilon &&
                Mathf.Abs(modelClip.EndTime - newEnd) <= TimeEpsilon &&
                Mathf.Abs(modelClip.EaseInDuration - newEaseIn) <= TimeEpsilon &&
                Mathf.Abs(modelClip.EaseOutDuration - newEaseOut) <= TimeEpsilon)
            {
                return;
            }

            Undo.RecordObject(showAsset, "Edit Stage Clip Timing");
            modelClip.StartTime = newStart;
            modelClip.EndTime = newEnd;
            modelClip.EaseInDuration = newEaseIn;
            modelClip.EaseOutDuration = newEaseOut;
            EditorUtility.SetDirty(showAsset);
        }

        public static void HandleTrackChanged(TrackAsset track)
        {
            if (IsSyncing || !(track is StageTrackAsset trackAsset))
            {
                return;
            }

            StageShowAsset showAsset = trackAsset.ShowAsset;
            if (showAsset == null || showAsset.Show == null)
            {
                return;
            }

            IStageTrack modelTrack = showAsset.Show.FindTrack(trackAsset.TrackId);
            if (modelTrack == null)
            {
                return;
            }

            var clipIdsOnTrack = new HashSet<string>();
            foreach (TimelineClip viewClip in trackAsset.GetClips())
            {
                if (viewClip?.asset is StageClipAsset clipAsset && !string.IsNullOrEmpty(clipAsset.ClipId))
                {
                    clipIdsOnTrack.Add(clipAsset.ClipId);
                    RegisterLiveClipAsset(clipAsset);
                }
            }

            var missingClips = new List<IStageClip>();
            IReadOnlyList<IStageClip> modelClips = modelTrack.Clips;
            for (int i = 0; i < modelClips.Count; i++)
            {
                IStageClip modelClip = modelClips[i];
                if (modelClip != null && !clipIdsOnTrack.Contains(modelClip.Id))
                {
                    missingClips.Add(modelClip);
                }
            }

            if (missingClips.Count == 0)
            {
                return;
            }

            TimelineAsset timelineAsset = trackAsset.timelineAsset;
            for (int i = 0; i < missingClips.Count; i++)
            {
                IStageClip missingClip = missingClips[i];
                if (TryFindClipOnOtherTrack(timelineAsset, showAsset, trackAsset, missingClip.Id, out StageTrackAsset otherTrackAsset, out TimelineClip movedViewClip))
                {
                    IStageTrack otherModelTrack = showAsset.Show.FindTrack(otherTrackAsset.TrackId);
                    if (otherModelTrack != null && otherModelTrack != modelTrack && otherModelTrack.CanAcceptClip(missingClip))
                    {
                        Undo.RecordObject(showAsset, "Move Stage Clip Between Tracks");
                        modelTrack.RemoveClip(missingClip.Id);
                        otherModelTrack.AddClip(missingClip);
                        missingClip.StartTime = (float)movedViewClip.start;
                        missingClip.EndTime = (float)movedViewClip.end;
                        EditorUtility.SetDirty(showAsset);
                    }

                    continue;
                }

                if (s_LiveClipAssets.TryGetValue(missingClip.Id, out StageClipAsset liveAsset) && liveAsset != null)
                {
                    continue;
                }

                s_LiveClipAssets.Remove(missingClip.Id);
                Undo.RecordObject(showAsset, "Delete Stage Clip");
                modelTrack.RemoveClip(missingClip.Id);
                EditorUtility.SetDirty(showAsset);
            }
        }

        public static string GetClipValidationError(TimelineClip clip)
        {
            if (clip == null || !(clip.GetParentTrack() is StageTrackAsset trackAsset) || !(clip.asset is StageClipAsset clipAsset))
            {
                return string.Empty;
            }

            StageShowAsset showAsset = trackAsset.ShowAsset;
            if (showAsset == null || showAsset.Show == null)
            {
                return string.Empty;
            }

            IStageTrack modelTrack = showAsset.Show.FindTrack(trackAsset.TrackId);
            if (modelTrack == null)
            {
                return "Track not found in StageShow.";
            }

            if (string.IsNullOrEmpty(clipAsset.ClipId))
            {
                return string.Empty;
            }

            if (modelTrack.FindClip(clipAsset.ClipId) != null)
            {
                return string.Empty;
            }

            IStageClip existingClip = showAsset.Show.FindClip(clipAsset.ClipId);
            if (existingClip != null && !modelTrack.CanAcceptClip(existingClip))
            {
                return "Clip module type does not match this track.";
            }

            return string.Empty;
        }

        public static void SyncShowToTimeline(
            StageShowAsset showAsset,
            TimelineAsset timelineAsset,
            bool recordUndo = true,
            string undoName = "Sync Stage Timeline")
        {
            if (showAsset == null || showAsset.Show == null || timelineAsset == null)
            {
                return;
            }

            foreach (TrackAsset track in timelineAsset.GetOutputTracks())
            {
                if (track is StageTrackAsset stageTrack && stageTrack.ShowAsset == showAsset)
                {
                    SyncTrackToView(stageTrack, recordUndo, undoName);
                }
            }
        }

        public static void SyncTrackToView(
            StageTrackAsset trackAsset,
            bool recordUndo = true,
            string undoName = "Sync Stage Track")
        {
            if (trackAsset == null || trackAsset.ShowAsset == null || trackAsset.ShowAsset.Show == null)
            {
                return;
            }

            StageShowAsset showAsset = trackAsset.ShowAsset;
            IStageTrack modelTrack = showAsset.Show.FindTrack(trackAsset.TrackId);
            if (modelTrack == null)
            {
                return;
            }

            bool prevSyncing = IsSyncing;
            IsSyncing = true;
            try
            {
                var viewClipsById = new Dictionary<string, TimelineClip>();
                var strayViewClips = new List<TimelineClip>();

                foreach (TimelineClip viewClip in trackAsset.GetClips())
                {
                    if (viewClip?.asset is StageClipAsset clipAsset && !string.IsNullOrEmpty(clipAsset.ClipId))
                    {
                        viewClipsById[clipAsset.ClipId] = viewClip;
                        RegisterLiveClipAsset(clipAsset);
                    }
                }

                bool trackModified = false;
                var activeModelIds = new HashSet<string>();
                IReadOnlyList<IStageClip> modelClips = modelTrack.Clips;

                for (int i = 0; i < modelClips.Count; i++)
                {
                    IStageClip modelClip = modelClips[i];
                    if (modelClip == null)
                    {
                        continue;
                    }

                    activeModelIds.Add(modelClip.Id);
                    float targetStart = modelClip.StartTime;
                    float targetDuration = Mathf.Max(MinClipDuration, modelClip.Duration);
                    float targetEaseIn = Mathf.Max(0f, modelClip.EaseInDuration);
                    float targetEaseOut = Mathf.Max(0f, modelClip.EaseOutDuration);

                    if (viewClipsById.TryGetValue(modelClip.Id, out TimelineClip viewClip))
                    {
                        if (Mathf.Abs((float)viewClip.start - targetStart) > TimeEpsilon ||
                            Mathf.Abs((float)viewClip.duration - targetDuration) > TimeEpsilon ||
                            Mathf.Abs((float)viewClip.easeInDuration - targetEaseIn) > TimeEpsilon ||
                            Mathf.Abs((float)viewClip.easeOutDuration - targetEaseOut) > TimeEpsilon)
                        {
                            if (recordUndo && !trackModified)
                            {
                                Undo.RecordObject(trackAsset, undoName);
                            }

                            viewClip.start = targetStart;
                            viewClip.duration = targetDuration;
                            viewClip.easeInDuration = targetEaseIn;
                            viewClip.easeOutDuration = targetEaseOut;
                            trackModified = true;
                        }
                    }
                    else
                    {
                        if (recordUndo && !trackModified)
                        {
                            Undo.RecordObject(trackAsset, undoName);
                        }

                        TimelineClip createdClip = trackAsset.CreateClip<StageClipAsset>();
                        createdClip.start = targetStart;
                        createdClip.duration = targetDuration;
                        createdClip.easeInDuration = targetEaseIn;
                        createdClip.easeOutDuration = targetEaseOut;

                        if (createdClip.asset is StageClipAsset createdAsset)
                        {
                            if (recordUndo)
                            {
                                Undo.RegisterCreatedObjectUndo(createdAsset, undoName);
                            }

                            createdAsset.ClipId = modelClip.Id;
                            RegisterLiveClipAsset(createdAsset);
                            EditorUtility.SetDirty(createdAsset);
                        }

                        trackModified = true;
                    }
                }

                foreach (KeyValuePair<string, TimelineClip> pair in viewClipsById)
                {
                    if (!activeModelIds.Contains(pair.Key) && showAsset.Show.FindClip(pair.Key) == null)
                    {
                        strayViewClips.Add(pair.Value);
                    }
                }

                if (strayViewClips.Count > 0 && trackAsset.timelineAsset != null)
                {
                    for (int i = 0; i < strayViewClips.Count; i++)
                    {
                        TimelineClip strayClip = strayViewClips[i];
                        if (strayClip.asset is StageClipAsset strayAsset && !string.IsNullOrEmpty(strayAsset.ClipId))
                        {
                            s_LiveClipAssets.Remove(strayAsset.ClipId);
                        }

                        trackAsset.timelineAsset.DeleteClip(strayClip);
                        trackModified = true;
                    }
                }

                if (trackModified)
                {
                    EditorUtility.SetDirty(trackAsset);
                }
            }
            finally
            {
                IsSyncing = prevSyncing;
            }
        }

        private static void HandleUndoRedoPerformed()
        {
            TimelineAsset inspectedAsset = TimelineEditor.inspectedAsset;
            if (inspectedAsset != null)
            {
                RebuildLiveClipCache(inspectedAsset);
            }

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

        private static void RebuildLiveClipCache(TimelineAsset timelineAsset)
        {
            s_LiveClipAssets.Clear();
            foreach (TrackAsset track in timelineAsset.GetOutputTracks())
            {
                if (!(track is StageTrackAsset stageTrack))
                {
                    continue;
                }

                foreach (TimelineClip clip in stageTrack.GetClips())
                {
                    if (clip?.asset is StageClipAsset clipAsset)
                    {
                        RegisterLiveClipAsset(clipAsset);
                    }
                }
            }
        }

        private static void RegisterLiveClipAsset(StageClipAsset clipAsset)
        {
            if (clipAsset != null && !string.IsNullOrEmpty(clipAsset.ClipId))
            {
                s_LiveClipAssets[clipAsset.ClipId] = clipAsset;
            }
        }

        private static bool TryFindClipOnOtherTrack(
            TimelineAsset timelineAsset,
            StageShowAsset showAsset,
            StageTrackAsset excludedTrack,
            string clipId,
            out StageTrackAsset foundTrack,
            out TimelineClip foundClip)
        {
            foundTrack = null;
            foundClip = null;

            if (timelineAsset == null || string.IsNullOrEmpty(clipId))
            {
                return false;
            }

            foreach (TrackAsset track in timelineAsset.GetOutputTracks())
            {
                if (!(track is StageTrackAsset stageTrack) || stageTrack == excludedTrack || stageTrack.ShowAsset != showAsset)
                {
                    continue;
                }

                foreach (TimelineClip viewClip in stageTrack.GetClips())
                {
                    if (viewClip?.asset is StageClipAsset clipAsset && clipAsset.ClipId == clipId)
                    {
                        foundTrack = stageTrack;
                        foundClip = viewClip;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
