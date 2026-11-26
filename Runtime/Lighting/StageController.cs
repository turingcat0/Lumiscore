using System;
using System.Collections.Generic;
using Lumiscore.Core;
using UnityEngine;

namespace Lumiscore.Lighting
{
    /// <summary>
    /// Scene-root runtime controller that builds the in-memory <see cref="LightNodeBase"/> hierarchy
    /// directly from scene <see cref="LightGroup"/> / <see cref="LightFixture"/> components (single
    /// source of truth), pre-allocates per-node <see cref="ILightState"/> buffers for zero-GC
    /// top-down evaluation, and deduplicates per-frame Timeline <c>ProcessFrame</c> calls.
    /// </summary>
    public class StageController : MonoBehaviour
    {
        private sealed class NodeEvalBuffers
        {
            public readonly ILightState[] PrevStates;
            public readonly ILightState[] WorkingStates;

            public NodeEvalBuffers(int childCount, Func<ILightState> stateFactory)
            {
                PrevStates = new ILightState[childCount];
                WorkingStates = new ILightState[childCount];
                for (int i = 0; i < childCount; i++)
                {
                    PrevStates[i] = stateFactory();
                    WorkingStates[i] = stateFactory();
                }
            }
        }

        [SerializeField] private StageShowAsset _showAsset;
        [SerializeField] private bool _wrapIndividualLights;

        private readonly List<LightNodeBase> _rootNodes = new List<LightNodeBase>();
        private readonly Dictionary<LightNodeBase, ILightState> _rootStates = new Dictionary<LightNodeBase, ILightState>();
        private readonly Dictionary<LightNodeBase, NodeEvalBuffers> _nodeBuffers = new Dictionary<LightNodeBase, NodeEvalBuffers>();

        private bool _cacheDirty = true;
        private ulong _lastFrameId = ulong.MaxValue;
        private float _lastEvaluatedTime = float.NaN;
        private int _activeGraphCount;

        public static event Action<StageController> AnyPreviewStarted;
        public static event Action<StageController> AnyPreviewStopped;

        public StageShowAsset ShowAsset
        {
            get => _showAsset;
            set
            {
                if (_showAsset != value)
                {
                    _showAsset = value;
                    InvalidateCache();
                }
            }
        }

        public bool WrapIndividualLights
        {
            get => _wrapIndividualLights;
            set
            {
                if (_wrapIndividualLights != value)
                {
                    _wrapIndividualLights = value;
                    InvalidateCache();
                }
            }
        }

        public IReadOnlyList<LightNodeBase> RootNodes
        {
            get
            {
                if (_cacheDirty)
                {
                    RebuildSceneTree();
                }

                return _rootNodes;
            }
        }

        public void InvalidateCache()
        {
            _cacheDirty = true;
            _lastFrameId = ulong.MaxValue;
            _lastEvaluatedTime = float.NaN;
        }

        public void NotifyGraphStart()
        {
            _activeGraphCount++;
            if (_activeGraphCount == 1)
            {
                AnyPreviewStarted?.Invoke(this);
            }
        }

        public void NotifyGraphStop()
        {
            if (_activeGraphCount <= 0)
            {
                return;
            }

            _activeGraphCount--;
            if (_activeGraphCount == 0)
            {
                AnyPreviewStopped?.Invoke(this);
            }
        }

        public void GatherAllPreviewProperties(IStagePropertyCollector collector)
        {
            if (collector == null)
            {
                return;
            }

            LightFixture[] fixtures = GetComponentsInChildren<LightFixture>(true);
            for (int i = 0; i < fixtures.Length; i++)
            {
                LightFixture fixture = fixtures[i];
                if (fixture != null && fixture.Driver != null)
                {
                    fixture.Driver.GatherPreviewProperties(fixture.gameObject, collector);
                }
            }
        }

        public void ResetAllPreviews()
        {
            LightFixture[] fixtures = GetComponentsInChildren<LightFixture>(true);
            for (int i = 0; i < fixtures.Length; i++)
            {
                LightFixture fixture = fixtures[i];
                if (fixture != null && fixture.Driver != null)
                {
                    fixture.Driver.ResetPreview(fixture.gameObject);
                }
            }
        }

        public void EvaluateFrame(float timelineTime, ulong frameId)
        {
            if (_lastFrameId == frameId && Mathf.Abs(_lastEvaluatedTime - timelineTime) <= 1e-6f)
            {
                return;
            }

            _lastFrameId = frameId;
            _lastEvaluatedTime = timelineTime;
            EvaluateAt(timelineTime);
        }

        public void EvaluateAt(float timelineTime, Action<LightNodeBase, ILightState> onLeafEvaluated = null)
        {
            StageShow show = _showAsset != null ? _showAsset.Show : null;
            if (show == null)
            {
                return;
            }

            if (_cacheDirty)
            {
                RebuildSceneTree();
            }

            for (int i = 0; i < _rootNodes.Count; i++)
            {
                LightNodeBase rootLightNode = _rootNodes[i];
                if (!_rootStates.TryGetValue(rootLightNode, out ILightState rootState) || rootState == null)
                {
                    continue;
                }

                rootState.ResetToDefault();
                EvaluateSubtree(show, rootLightNode, rootState, timelineTime, onLeafEvaluated);
            }
        }

        /// <summary>
        /// Scans child <see cref="LightGroup"/> and <see cref="LightFixture"/> components to build the
        /// runtime <see cref="RootNodes"/> hierarchy and pre-allocate per-node evaluation state buffers.
        /// </summary>
        public void RebuildSceneTree()
        {
            _cacheDirty = false;
            _rootNodes.Clear();
            _rootStates.Clear();
            _nodeBuffers.Clear();

            CollectChildNodes(transform, _rootNodes, _wrapIndividualLights);

            for (int i = 0; i < _rootNodes.Count; i++)
            {
                LightNodeBase rootLightNode = _rootNodes[i];
                Type stateType = ResolveTreeStateType(rootLightNode);
                if (stateType == null || !typeof(ILightState).IsAssignableFrom(stateType))
                {
                    continue;
                }

                Func<ILightState> factory = () => (ILightState)Activator.CreateInstance(stateType);
                _rootStates[rootLightNode] = factory();
                AllocateSubtreeBuffers(rootLightNode, factory);
            }
        }

        public static Type ResolveTreeStateType(LightNodeBase node)
        {
            if (node == null)
            {
                return null;
            }

            if (node is LightLeafNode leaf && leaf.Fixture != null && leaf.Fixture.StateType != null)
            {
                return leaf.Fixture.StateType;
            }

            IReadOnlyList<LightNodeBase> children = node.Children;
            if (children != null)
            {
                for (int i = 0; i < children.Count; i++)
                {
                    Type childType = ResolveTreeStateType(children[i]);
                    if (childType != null)
                    {
                        return childType;
                    }
                }
            }

            return null;
        }

        private static void CollectChildNodes(Transform parent, List<LightNodeBase> output, bool wrapIndividualLights)
        {
            int childCount = parent.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = parent.GetChild(i);

                LightGroup groupComp = child.GetComponent<LightGroup>();
                if (groupComp != null)
                {
                    groupComp.EnsureNodeId();
                    var groupNode = new LightGroupNode(child.name, groupComp.NodeId, child)
                    {
                        Layout = groupComp.Layout
                    };

                    var nestedChildren = new List<LightNodeBase>();
                    CollectChildNodes(child, nestedChildren, wrapIndividualLights);
                    for (int c = 0; c < nestedChildren.Count; c++)
                    {
                        groupNode.AddChild(nestedChildren[c]);
                    }

                    output.Add(groupNode);
                    continue;
                }

                LightFixture fixtureComp = child.GetComponent<LightFixture>();
                if (fixtureComp != null)
                {
                    fixtureComp.EnsureNodeId();
                    var leafNode = new LightLeafNode(child.name, fixtureComp.NodeId, child)
                    {
                        Fixture = fixtureComp
                    };

                    if (wrapIndividualLights)
                    {
                        string wrapperId = fixtureComp.NodeId + "_wrap";
                        var wrapper = new LightGroupNode(new LightNodeBase[] { leafNode }, child.name, wrapperId, child);
                        output.Add(wrapper);
                    }
                    else
                    {
                        output.Add(leafNode);
                    }

                    continue;
                }

                // Transparent organizational folder GameObject: continue scanning children
                CollectChildNodes(child, output, wrapIndividualLights);
            }
        }

        private void AllocateSubtreeBuffers(LightNodeBase node, Func<ILightState> stateFactory)
        {
            if (node == null)
            {
                return;
            }

            IReadOnlyList<LightNodeBase> children = node.Children;
            int childCount = children != null ? children.Count : 0;
            if (childCount == 0)
            {
                return;
            }

            _nodeBuffers[node] = new NodeEvalBuffers(childCount, stateFactory);
            for (int i = 0; i < childCount; i++)
            {
                AllocateSubtreeBuffers(children[i], stateFactory);
            }
        }

        private void EvaluateSubtree(
            StageShow show,
            LightNodeBase node,
            ILightState inheritedState,
            float timelineTime,
            Action<LightNodeBase, ILightState> onLeafEvaluated)
        {
            IReadOnlyList<LightNodeBase> children = node.Children;
            int childCount = children != null ? children.Count : 0;

            if (childCount == 0)
            {
                if (node is LightLeafNode leaf && leaf.Fixture?.Driver != null)
                {
                    leaf.Fixture.Driver.ApplyState(inheritedState);
                }

                onLeafEvaluated?.Invoke(node, inheritedState);
                return;
            }

            if (!_nodeBuffers.TryGetValue(node, out NodeEvalBuffers buffers))
            {
                return;
            }

            for (int i = 0; i < childCount; i++)
            {
                buffers.PrevStates[i].CopyFrom(inheritedState);
                buffers.WorkingStates[i].CopyFrom(inheritedState);
            }

            IReadOnlyList<IStageTrack> nodeTracks = show.GetTracks(node.Id);
            for (int i = 0; i < nodeTracks.Count; i++)
            {
                if (nodeTracks[i] is ILightTrack lightTrack)
                {
                    lightTrack.EvaluateTrack(
                        node,
                        timelineTime,
                        show.TempoMap,
                        children,
                        buffers.PrevStates,
                        buffers.WorkingStates);
                }
            }

            for (int i = 0; i < childCount; i++)
            {
                EvaluateSubtree(show, children[i], buffers.WorkingStates[i], timelineTime, onLeafEvaluated);
            }
        }
    }
}
