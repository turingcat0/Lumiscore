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
    /// Synchronizes scene <see cref="LightGroup"/> / <see cref="LightFixture"/> hierarchies into a
    /// <see cref="StageShowAsset"/> (preserving existing node identities and tracks), auto-generates
    /// typed <see cref="LightTrack{TModule}"/> instances from <see cref="IHasLightModule{TModule}"/>,
    /// and builds nested Unity Timeline <see cref="GroupTrack"/> / <see cref="StageTrackAsset"/> views.
    /// </summary>
    public static class StageSceneSync
    {
        public static void SyncTopologyFromScene(StageController controller, bool wrapIndividualLights = false)
        {
            if (controller == null || controller.ShowAsset == null || controller.ShowAsset.Show == null)
            {
                return;
            }

            StageShowAsset showAsset = controller.ShowAsset;
            StageShow show = showAsset.Show;

            Undo.RecordObject(showAsset, "Sync Stage Topology From Scene");

            var existingById = new Dictionary<string, LightNodeBase>();
            var preservedNonLightRoots = new List<IStageNode>();
            for (int i = 0; i < show.RootNodes.Count; i++)
            {
                if (show.RootNodes[i] is LightNodeBase lightRoot)
                {
                    CollectExistingNodes(lightRoot, existingById);
                }
                else if (show.RootNodes[i] != null)
                {
                    preservedNonLightRoots.Add(show.RootNodes[i]);
                }
            }

            var syncedLightRoots = new List<LightNodeBase>();
            CollectChildNodes(controller.transform, syncedLightRoots, existingById, wrapIndividualLights);

            // Replace root node list while keeping non-light roots intact
            while (show.RootNodes.Count > 0)
            {
                show.RemoveRootNode(show.RootNodes[show.RootNodes.Count - 1]);
            }

            for (int i = 0; i < preservedNonLightRoots.Count; i++)
            {
                show.AddRootNode(preservedNonLightRoots[i]);
            }

            for (int i = 0; i < syncedLightRoots.Count; i++)
            {
                show.AddRootNode(syncedLightRoots[i]);
            }

            EditorUtility.SetDirty(showAsset);
            controller.InvalidateCache();
            controller.RebuildEvaluationCache(show);
        }

        public static void EnsureTracksForAllGroups(StageController controller)
        {
            if (controller == null || controller.ShowAsset == null || controller.ShowAsset.Show == null)
            {
                return;
            }

            StageShowAsset showAsset = controller.ShowAsset;
            StageShow show = showAsset.Show;

            Undo.RecordObject(showAsset, "Generate Stage Tracks From State Modules");

            IReadOnlyList<IStageNode> roots = show.RootNodes;
            for (int i = 0; i < roots.Count; i++)
            {
                if (!(roots[i] is LightNodeBase rootLightNode))
                {
                    continue;
                }

                Type stateType = ResolveTreeStateType(rootLightNode);
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
            IReadOnlyList<IStageTrack> existingTracks = show.GetTracks(targetGroup);

            for (int i = 0; i < moduleTypes.Count; i++)
            {
                Type moduleType = moduleTypes[i];
                if (HasTrackForModule(existingTracks, moduleType))
                {
                    continue;
                }

                Type trackType = typeof(LightTrack<>).MakeGenericType(moduleType);
                var track = Activator.CreateInstance(trackType, targetGroup, null) as IStageTrack;
                if (track != null)
                {
                    show.AddTrack(track);
                }
            }
        }

        public static void SyncHierarchyToTimeline(StageShowAsset showAsset, TimelineAsset timelineAsset)
        {
            if (showAsset == null || showAsset.Show == null || timelineAsset == null)
            {
                return;
            }

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

            IReadOnlyList<IStageNode> roots = show.RootNodes;
            for (int i = 0; i < roots.Count; i++)
            {
                if (roots[i] is LightGroupNode rootGroup)
                {
                    SyncGroupNodeToTimeline(showAsset, show, timelineAsset, null, rootGroup, existingStageTracksById);
                }
            }

            EditorUtility.SetDirty(timelineAsset);
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

            IReadOnlyList<IStageTrack> modelTracks = show.GetTracks(groupNode);
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

        private static Type ResolveTreeStateType(LightNodeBase node)
        {
            if (node is LightLeafNode leaf && leaf.Fixture != null && leaf.Fixture.StateType != null)
            {
                return leaf.Fixture.StateType;
            }

            IReadOnlyList<LightNodeBase> children = node.Children;
            for (int i = 0; i < children.Count; i++)
            {
                Type found = ResolveTreeStateType(children[i]);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void CollectExistingNodes(LightNodeBase node, Dictionary<string, LightNodeBase> map)
        {
            if (node == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(node.Id))
            {
                map[node.Id] = node;
            }

            IReadOnlyList<LightNodeBase> children = node.Children;
            for (int i = 0; i < children.Count; i++)
            {
                CollectExistingNodes(children[i], map);
            }
        }

        private static void CollectChildNodes(
            Transform parent,
            List<LightNodeBase> output,
            Dictionary<string, LightNodeBase> existingById,
            bool wrapIndividualLights)
        {
            int childCount = parent.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = parent.GetChild(i);

                LightGroup groupComp = child.GetComponent<LightGroup>();
                if (groupComp != null)
                {
                    groupComp.EnsureNodeId();
                    LightGroupNode groupNode = existingById.TryGetValue(groupComp.NodeId, out LightNodeBase existing) &&
                                               existing is LightGroupNode reusedGroup
                        ? reusedGroup
                        : new LightGroupNode(child.name, groupComp.NodeId, child);

                    groupNode.Name = child.name;
                    groupNode.RootTransform = child;
                    groupNode.Layout = groupComp.Layout;

                    var nestedChildren = new List<LightNodeBase>();
                    CollectChildNodes(child, nestedChildren, existingById, wrapIndividualLights);
                    ReplaceGroupChildren(groupNode, nestedChildren);

                    output.Add(groupNode);
                    continue;
                }

                LightFixture fixtureComp = child.GetComponent<LightFixture>();
                if (fixtureComp != null)
                {
                    fixtureComp.EnsureNodeId();
                    LightLeafNode leafNode = existingById.TryGetValue(fixtureComp.NodeId, out LightNodeBase existing) &&
                                             existing is LightLeafNode reusedLeaf
                        ? reusedLeaf
                        : new LightLeafNode(child.name, fixtureComp.NodeId, child);

                    leafNode.Name = child.name;
                    leafNode.RootTransform = child;
                    leafNode.Fixture = fixtureComp;

                    if (wrapIndividualLights)
                    {
                        string wrapperId = fixtureComp.NodeId + "_wrap";
                        LightGroupNode wrapper = existingById.TryGetValue(wrapperId, out LightNodeBase existingWrap) &&
                                                 existingWrap is LightGroupNode reusedWrap
                            ? reusedWrap
                            : new LightGroupNode(child.name, wrapperId, child);

                        wrapper.Name = child.name;
                        wrapper.RootTransform = child;
                        ReplaceGroupChildren(wrapper, new List<LightNodeBase> { leafNode });
                        output.Add(wrapper);
                    }
                    else
                    {
                        output.Add(leafNode);
                    }

                    continue;
                }

                // Transparent organizational folder GameObject: continue scanning its children
                CollectChildNodes(child, output, existingById, wrapIndividualLights);
            }
        }

        private static void ReplaceGroupChildren(LightGroupNode group, List<LightNodeBase> newChildren)
        {
            while (group.Children.Count > 0)
            {
                group.RemoveChild(group.Children[group.Children.Count - 1]);
            }

            for (int i = 0; i < newChildren.Count; i++)
            {
                group.AddChild(newChildren[i]);
            }
        }
    }
}
