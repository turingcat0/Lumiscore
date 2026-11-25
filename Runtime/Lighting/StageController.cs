using System;
using System.Collections.Generic;
using Lumiscore.Core;
using UnityEngine;

namespace Lumiscore.Lighting
{
    [Serializable]
    public sealed class StageNodeBinding
    {
        [SerializeField] private string _nodeId;
        [SerializeField] private Transform _rootTransform;
        [SerializeField] private MonoBehaviour _driverComponent;
        [SerializeField] private string _stateTypeName;

        public string NodeId
        {
            get => _nodeId;
            set => _nodeId = value ?? string.Empty;
        }

        public Transform RootTransform
        {
            get => _rootTransform;
            set => _rootTransform = value;
        }

        public MonoBehaviour DriverComponent
        {
            get => _driverComponent;
            set => _driverComponent = value;
        }

        public IStageDriver Driver => _driverComponent as IStageDriver;

        public string StateTypeName
        {
            get => _stateTypeName;
            set => _stateTypeName = value ?? string.Empty;
        }

        public Type ResolveStateType()
        {
            if (Driver != null && Driver.StateType != null)
            {
                return Driver.StateType;
            }

            return string.IsNullOrEmpty(_stateTypeName) ? null : Type.GetType(_stateTypeName);
        }
    }

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
        [SerializeField] private List<StageNodeBinding> _bindings = new List<StageNodeBinding>();

        private readonly Dictionary<string, StageNodeBinding> _bindingLookup = new Dictionary<string, StageNodeBinding>();
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

        public IReadOnlyList<StageNodeBinding> Bindings => _bindings;

        public void InvalidateCache()
        {
            _cacheDirty = true;
            _lastFrameId = ulong.MaxValue;
            _lastEvaluatedTime = float.NaN;
        }

        public void BindNode(string nodeId, Transform rootTransform, MonoBehaviour driverComponent = null, Type stateType = null)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                return;
            }

            StageNodeBinding existing = FindBinding(nodeId);
            if (existing == null)
            {
                existing = new StageNodeBinding { NodeId = nodeId };
                _bindings.Add(existing);
            }

            existing.RootTransform = rootTransform;
            existing.DriverComponent = driverComponent;
            if (stateType != null)
            {
                existing.StateTypeName = stateType.AssemblyQualifiedName;
            }

            InvalidateCache();
        }

        public StageNodeBinding FindBinding(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                return null;
            }

            for (int i = 0; i < _bindings.Count; i++)
            {
                if (_bindings[i] != null && _bindings[i].NodeId == nodeId)
                {
                    return _bindings[i];
                }
            }

            return null;
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

        public void CaptureAllBoundStates(Dictionary<string, IStageState> target)
        {
            if (target == null)
            {
                return;
            }

            target.Clear();
            for (int i = 0; i < _bindings.Count; i++)
            {
                StageNodeBinding binding = _bindings[i];
                if (binding == null || string.IsNullOrEmpty(binding.NodeId) || binding.Driver == null)
                {
                    continue;
                }

                target[binding.NodeId] = binding.Driver.CaptureState();
            }
        }

        public void ApplyCapturedStates(IReadOnlyDictionary<string, IStageState> snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            foreach (KeyValuePair<string, IStageState> pair in snapshot)
            {
                StageNodeBinding binding = FindBinding(pair.Key);
                if (binding?.Driver != null && pair.Value != null)
                {
                    binding.Driver.ApplyState(pair.Value);
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
                RebuildEvaluationCache(show);
            }

            IReadOnlyList<IStageNode> roots = show.RootNodes;
            for (int i = 0; i < roots.Count; i++)
            {
                if (!(roots[i] is LightNodeBase rootLightNode))
                {
                    continue;
                }

                if (!_rootStates.TryGetValue(rootLightNode, out ILightState rootState) || rootState == null)
                {
                    continue;
                }

                rootState.ResetToDefault();
                EvaluateSubtree(show, rootLightNode, rootState, timelineTime, onLeafEvaluated);
            }
        }

        public void RebuildEvaluationCache(StageShow show = null)
        {
            _cacheDirty = false;
            _bindingLookup.Clear();
            _rootStates.Clear();
            _nodeBuffers.Clear();

            for (int i = 0; i < _bindings.Count; i++)
            {
                StageNodeBinding binding = _bindings[i];
                if (binding != null && !string.IsNullOrEmpty(binding.NodeId))
                {
                    _bindingLookup[binding.NodeId] = binding;
                }
            }

            StageShow targetShow = show ?? (_showAsset != null ? _showAsset.Show : null);
            if (targetShow == null)
            {
                return;
            }

            IReadOnlyList<IStageNode> roots = targetShow.RootNodes;
            for (int i = 0; i < roots.Count; i++)
            {
                if (!(roots[i] is LightNodeBase rootLightNode))
                {
                    continue;
                }

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

        private void AllocateSubtreeBuffers(LightNodeBase node, Func<ILightState> stateFactory)
        {
            if (node == null)
            {
                return;
            }

            if (_bindingLookup.TryGetValue(node.Id, out StageNodeBinding binding) && binding.RootTransform != null)
            {
                node.RootTransform = binding.RootTransform;
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

        private Type ResolveTreeStateType(LightNodeBase node)
        {
            if (node == null)
            {
                return null;
            }

            if (_bindingLookup.TryGetValue(node.Id, out StageNodeBinding binding))
            {
                Type direct = binding.ResolveStateType();
                if (direct != null)
                {
                    return direct;
                }
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
                if (_bindingLookup.TryGetValue(node.Id, out StageNodeBinding binding) && binding.Driver != null)
                {
                    binding.Driver.ApplyState(inheritedState);
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

            IReadOnlyList<IStageTrack> nodeTracks = show.GetTracks(node);
            for (int i = 0; i < nodeTracks.Count; i++)
            {
                if (nodeTracks[i] is ILightTrack lightTrack)
                {
                    lightTrack.EvaluateTrack(
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
