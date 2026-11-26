using System;
using System.Collections.Generic;
using Lumiscore.Core;
using Lumiscore.Editor.Timeline;
using Lumiscore.Lighting;
using Lumiscore.Timeline;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

namespace Lumiscore.Editor
{
    /// <summary>
    /// Editor utilities for auto-detecting scene <see cref="LightFixture"/> drivers via
    /// <see cref="StageDriverBase.TryBind"/>, generating typed <see cref="LightTrack{TModule}"/>
    /// tracks from <see cref="IHasLightModule{TModule}"/>, and projecting <see cref="StageController.RootNodes"/>
    /// into nested Unity Timeline <see cref="GroupTrack"/> / <see cref="StageTrackAsset"/> views.
    /// </summary>
    public static class StageSceneSync
    {
        /// <summary>
        /// Scans the hierarchy under <paramref name="controller"/> and automatically attaches
        /// <see cref="LightFixture"/> with a bound <see cref="StageDriverBase"/> to any matching
        /// GameObject. Logs a warning and skips objects matched by multiple drivers.
        /// </summary>
        public static int AutoDetectFixtures(StageController controller)
        {
            if (controller == null)
            {
                return 0;
            }

            IReadOnlyList<Type> driverTypes = StageTypeRegistry.GetConcreteTypesDerivedFrom(typeof(StageDriverBase));
            if (driverTypes.Count == 0)
            {
                return 0;
            }

            int detectedCount = 0;
            AutoDetectRecursive(controller.transform, driverTypes, ref detectedCount);

            if (detectedCount > 0)
            {
                controller.InvalidateCache();
            }

            return detectedCount;
        }

        public static void EnsureTracksForAllGroups(StageController controller)
        {
            if (controller == null || controller.ShowAsset == null || controller.ShowAsset.Show == null)
            {
                return;
            }

            controller.RebuildSceneTree();

            StageShowAsset showAsset = controller.ShowAsset;
            StageShow show = showAsset.Show;

            Undo.RecordObject(showAsset, "Generate Stage Tracks From State Modules");

            IReadOnlyList<LightNodeBase> roots = controller.RootNodes;
            for (int i = 0; i < roots.Count; i++)
            {
                LightNodeBase rootLightNode = roots[i];
                Type stateType = StageController.ResolveTreeStateType(rootLightNode);
                if (stateType == null)
                {
                    continue;
                }

                EnsureTracksForSubtree(show, rootLightNode, stateType);
            }

            EditorUtility.SetDirty(showAsset);
        }

        public static void EnsureTracksForGroup(StageShow show, LightGroupNode targetGroup, Type stateType)
        {
            if (show == null || targetGroup == null || stateType == null)
            {
                return;
            }

            IReadOnlyList<Type> moduleTypes = LightTypeCatalog.GetModuleTypesForState(stateType);
            IReadOnlyList<IStageTrack> existingTracks = show.GetTracks(targetGroup.Id);

            for (int i = 0; i < moduleTypes.Count; i++)
            {
                Type moduleType = moduleTypes[i];
                if (HasTrackForModule(existingTracks, moduleType))
                {
                    continue;
                }

                Type trackType = typeof(LightTrack<>).MakeGenericType(moduleType);
                var track = Activator.CreateInstance(trackType, targetGroup.Id, null) as IStageTrack;
                if (track != null)
                {
                    show.AddTrack(track);
                }
            }
        }

        public static void SyncHierarchyToTimeline(StageController controller, TimelineAsset timelineAsset)
        {
            if (controller == null || controller.ShowAsset == null || controller.ShowAsset.Show == null || timelineAsset == null)
            {
                return;
            }

            controller.RebuildSceneTree();

            StageShowAsset showAsset = controller.ShowAsset;
            StageShow show = showAsset.Show;

            var existingStageTracksById = new Dictionary<string, StageTrackAsset>();
            foreach (TrackAsset track in timelineAsset.GetOutputTracks())
            {
                if (track is StageTrackAsset stageTrack &&
                    stageTrack.ShowAsset == showAsset &&
                    !string.IsNullOrEmpty(stageTrack.TrackId))
                {
                    existingStageTracksById[stageTrack.TrackId] = stageTrack;
                }
            }

            IReadOnlyList<LightNodeBase> roots = controller.RootNodes;
            for (int i = 0; i < roots.Count; i++)
            {
                if (roots[i] is LightGroupNode rootGroup)
                {
                    SyncGroupNodeToTimeline(showAsset, show, timelineAsset, null, rootGroup, existingStageTracksById);
                }
            }

            EditorUtility.SetDirty(timelineAsset);
        }

        private static void AutoDetectRecursive(
            Transform parent,
            IReadOnlyList<Type> driverTypes,
            ref int detectedCount)
        {
            int childCount = parent.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = parent.GetChild(i);
                GameObject go = child.gameObject;

                // Do not auto-detect LightGroup containers as fixtures; recurse into their children
                if (go.GetComponent<LightGroup>() != null)
                {
                    AutoDetectRecursive(child, driverTypes, ref detectedCount);
                    continue;
                }

                LightFixture existingFixture = go.GetComponent<LightFixture>();
                if (existingFixture != null && existingFixture.Driver != null)
                {
                    // Already bound fixture; do not recurse inside fixture internals
                    continue;
                }

                var matchedDrivers = new List<StageDriverBase>();
                for (int d = 0; d < driverTypes.Count; d++)
                {
                    Type driverType = driverTypes[d];
                    if (driverType.GetConstructor(Type.EmptyTypes) == null)
                    {
                        continue;
                    }

                    if (Activator.CreateInstance(driverType) is StageDriverBase candidate && candidate.TryBind(go))
                    {
                        matchedDrivers.Add(candidate);
                    }
                }

                if (matchedDrivers.Count == 1)
                {
                    LightFixture fixture = existingFixture != null
                        ? existingFixture
                        : Undo.AddComponent<LightFixture>(go);
                    Undo.RecordObject(fixture, "Auto-Detect Light Fixture");
                    fixture.EnsureNodeId();
                    fixture.Driver = matchedDrivers[0];
                    EditorUtility.SetDirty(fixture);
                    detectedCount++;
                    continue;
                }

                if (matchedDrivers.Count > 1)
                {
                    var names = new List<string>(matchedDrivers.Count);
                    for (int m = 0; m < matchedDrivers.Count; m++)
                    {
                        names.Add(matchedDrivers[m].GetType().Name);
                    }

                    Debug.LogWarning(
                        $"[Lumiscore] Ambiguous fixture auto-detection on '{go.name}': matched multiple drivers " +
                        $"({string.Join(", ", names)}). Please assign the intended driver manually.",
                        go);
                    continue;
                }

                // Not a fixture: continue scanning children (e.g. organizational folder GameObject)
                AutoDetectRecursive(child, driverTypes, ref detectedCount);
            }
        }

        private static void SyncGroupNodeToTimeline(
            StageShowAsset showAsset,
            StageShow show,
            TimelineAsset timelineAsset,
            TrackAsset parentGroupTrack,
            LightGroupNode groupNode,
            Dictionary<string, StageTrackAsset> existingStageTracksById)
        {
            GroupTrack groupTrack = FindOrCreateGroupTrack(timelineAsset, parentGroupTrack, groupNode.Name);

            IReadOnlyList<IStageTrack> modelTracks = show.GetTracks(groupNode.Id);
            for (int i = 0; i < modelTracks.Count; i++)
            {
                IStageTrack modelTrack = modelTracks[i];
                if (modelTrack == null)
                {
                    continue;
                }

                if (!existingStageTracksById.TryGetValue(modelTrack.Id, out StageTrackAsset viewTrack) || viewTrack == null)
                {
                    string trackName = modelTrack.ModuleType != null ? modelTrack.ModuleType.Name : "StageTrack";
                    viewTrack = timelineAsset.CreateTrack<StageTrackAsset>(groupTrack, trackName);
                    viewTrack.ShowAsset = showAsset;
                    viewTrack.TrackId = modelTrack.Id;
                    viewTrack.ModuleTypeName = modelTrack.ModuleType != null
                        ? modelTrack.ModuleType.AssemblyQualifiedName
                        : string.Empty;
                    existingStageTracksById[modelTrack.Id] = viewTrack;
                }

                StageTimelineSync.SyncTrackToView(viewTrack, recordUndo: false);
            }

            IReadOnlyList<LightNodeBase> children = groupNode.Children;
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i] is LightGroupNode childGroup)
                {
                    SyncGroupNodeToTimeline(showAsset, show, timelineAsset, groupTrack, childGroup, existingStageTracksById);
                }
            }
        }

        private static GroupTrack FindOrCreateGroupTrack(
            TimelineAsset timelineAsset,
            TrackAsset parentGroupTrack,
            string groupName)
        {
            string safeName = string.IsNullOrEmpty(groupName) ? "LightGroup" : groupName;
            IEnumerable<TrackAsset> siblings = parentGroupTrack != null
                ? parentGroupTrack.GetChildTracks()
                : timelineAsset.GetRootTracks();

            foreach (TrackAsset sibling in siblings)
            {
                if (sibling is GroupTrack existing && existing.name == safeName)
                {
                    return existing;
                }
            }

            return timelineAsset.CreateTrack<GroupTrack>(parentGroupTrack, safeName);
        }

        private static void EnsureTracksForSubtree(StageShow show, LightNodeBase node, Type stateType)
        {
            if (node is LightGroupNode group)
            {
                EnsureTracksForGroup(show, group, stateType);
            }

            IReadOnlyList<LightNodeBase> children = node.Children;
            for (int i = 0; i < children.Count; i++)
            {
                EnsureTracksForSubtree(show, children[i], stateType);
            }
        }

        private static bool HasTrackForModule(IReadOnlyList<IStageTrack> tracks, Type moduleType)
        {
            for (int i = 0; i < tracks.Count; i++)
            {
                if (tracks[i] != null && tracks[i].ModuleType == moduleType)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
