using System;
using System.Collections.Generic;
using Lumiscore.Core;

namespace Lumiscore.Lighting
{
    public sealed class LightShowEvaluator
    {
        private readonly Dictionary<IStageNode, List<ILightTrack>> _tracksByNode =
            new Dictionary<IStageNode, List<ILightTrack>>();

        public void Evaluate(
            StageShow show,
            float timelineTime,
            Action<ILightNode, ILightState> onLeafEvaluated,
            Func<Type, ILightState> defaultStateFactory = null)
        {
            if (show == null || onLeafEvaluated == null)
            {
                return;
            }

            IndexTracks(show);

            IReadOnlyList<IStageNode> roots = show.RootNodes;
            for (int i = 0; i < roots.Count; i++)
            {
                if (!(roots[i] is ILightNode rootLightNode))
                {
                    continue;
                }

                Type stateType = rootLightNode.ResolveStateType();
                ILightState rootState = CreateDefaultState(stateType, defaultStateFactory);
                if (rootState == null)
                {
                    continue;
                }

                EvaluateSubtree(rootLightNode, rootState, show.TempoMap, timelineTime, onLeafEvaluated);
            }
        }

        private void IndexTracks(StageShow show)
        {
            foreach (List<ILightTrack> list in _tracksByNode.Values)
            {
                list.Clear();
            }

            IReadOnlyList<IStageTrack> tracks = show.Tracks;
            for (int i = 0; i < tracks.Count; i++)
            {
                if (!(tracks[i] is ILightTrack lightTrack) || lightTrack.TargetNode == null)
                {
                    continue;
                }

                if (!_tracksByNode.TryGetValue(lightTrack.TargetNode, out List<ILightTrack> list))
                {
                    list = new List<ILightTrack>();
                    _tracksByNode[lightTrack.TargetNode] = list;
                }

                list.Add(lightTrack);
            }
        }

        private void EvaluateSubtree(
            ILightNode node,
            ILightState inheritedState,
            StageTempoMap tempoMap,
            float timelineTime,
            Action<ILightNode, ILightState> onLeafEvaluated)
        {
            if (node is LightGroupNode group)
            {
                IReadOnlyList<ILightNode> children = group.Children;
                int childCount = children.Count;
                if (childCount == 0)
                {
                    return;
                }

                var prevStates = new ILightState[childCount];
                var workingStates = new ILightState[childCount];
                for (int i = 0; i < childCount; i++)
                {
                    prevStates[i] = inheritedState.Clone();
                    workingStates[i] = inheritedState.Clone();
                }

                ApplyNodeTracks(node, timelineTime, tempoMap, children, prevStates, workingStates);

                for (int i = 0; i < childCount; i++)
                {
                    EvaluateSubtree(children[i], workingStates[i], tempoMap, timelineTime, onLeafEvaluated);
                }
            }
            else
            {
                var singleChild = new[] { node };
                var prevStates = new[] { inheritedState.Clone() };
                var workingStates = new[] { inheritedState.Clone() };

                ApplyNodeTracks(node, timelineTime, tempoMap, singleChild, prevStates, workingStates);
                onLeafEvaluated(node, workingStates[0]);
            }
        }

        private void ApplyNodeTracks(
            ILightNode node,
            float timelineTime,
            StageTempoMap tempoMap,
            IReadOnlyList<ILightNode> children,
            IReadOnlyList<ILightState> prevStates,
            IReadOnlyList<ILightState> workingStates)
        {
            if (!_tracksByNode.TryGetValue(node, out List<ILightTrack> tracks) || tracks.Count == 0)
            {
                return;
            }

            for (int i = 0; i < tracks.Count; i++)
            {
                tracks[i].EvaluateTrack(timelineTime, tempoMap, children, prevStates, workingStates);
            }
        }

        private static ILightState CreateDefaultState(Type stateType, Func<Type, ILightState> factory)
        {
            if (factory != null)
            {
                return factory(stateType);
            }

            if (stateType == null || !typeof(ILightState).IsAssignableFrom(stateType))
            {
                return null;
            }

            return Activator.CreateInstance(stateType) as ILightState;
        }
    }
}
