using System;
using System.Collections.Generic;
using Lumiscore.Core;
using UnityEngine;

namespace Lumiscore.Lighting
{
    [Serializable]
    public class LightTrack<TModule> : ILightTrack<TModule>
        where TModule : class, ILightStateModule
    {
        [SerializeField] private string _id;
        [SerializeField] private string _targetNodeId;
        [SerializeField] private List<LightClip<TModule>> _clips = new List<LightClip<TModule>>();

        public string Id => _id;

        public string TargetNodeId
        {
            get => _targetNodeId;
            set => _targetNodeId = value ?? string.Empty;
        }

        public Type ModuleType => typeof(TModule);

        public List<LightClip<TModule>> Clips => _clips;

        IReadOnlyList<IStageClip> IStageTrack.Clips => _clips;

        public LightTrack()
            : this((string)null, null)
        {
        }

        public LightTrack(string targetNodeId, string id = null)
        {
            _id = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString("N") : id;
            _targetNodeId = targetNodeId ?? string.Empty;
        }

        public LightTrack(LightNodeBase targetNode, string id = null)
            : this(targetNode?.Id, id)
        {
        }

        public IStageClip FindClip(string clipId)
        {
            if (string.IsNullOrEmpty(clipId))
            {
                return null;
            }

            for (int i = 0; i < _clips.Count; i++)
            {
                if (_clips[i] != null && _clips[i].Id == clipId)
                {
                    return _clips[i];
                }
            }

            return null;
        }

        public IStageClip CreateClip(float startTime, float endTime, string id = null)
        {
            var clip = new LightClip<TModule>(id)
            {
                StartTime = startTime,
                EndTime = endTime
            };
            _clips.Add(clip);
            return clip;
        }

        public IStageClip CloneClip(IStageClip source, float startTime, float endTime, string id = null)
        {
            if (!(source is LightClip<TModule> sourceClip))
            {
                return null;
            }

            var clone = new LightClip<TModule>(id)
            {
                StartTime = startTime,
                EndTime = endTime,
                BlendMode = sourceClip.BlendMode,
                Weight = sourceClip.Weight,
                EaseInDuration = sourceClip.EaseInDuration,
                EaseOutDuration = sourceClip.EaseOutDuration,
                Effect = CloneEffect(sourceClip.Effect)
            };
            _clips.Add(clone);
            return clone;
        }

        public bool CanAcceptClip(IStageClip clip)
        {
            return clip is LightClip<TModule>;
        }

        public bool AddClip(IStageClip clip)
        {
            if (!(clip is LightClip<TModule> lightClip) || _clips.Contains(lightClip))
            {
                return false;
            }

            _clips.Add(lightClip);
            return true;
        }

        public bool RemoveClip(string clipId)
        {
            if (string.IsNullOrEmpty(clipId))
            {
                return false;
            }

            for (int i = 0; i < _clips.Count; i++)
            {
                if (_clips[i] != null && _clips[i].Id == clipId)
                {
                    _clips.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public void Restore(string id, string targetNodeId)
        {
            _id = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString("N") : id;
            _targetNodeId = targetNodeId ?? string.Empty;
        }

        [NonSerialized] private readonly List<LightClip<TModule>> _activeClips = new List<LightClip<TModule>>();
        [NonSerialized] private TModule[] _evalBuffer;
        [NonSerialized] private TModule[] _secondaryEvalBuffer;
        [NonSerialized] private TModule[] _mixBuffer;

        public void EvaluateTrack(
            LightNodeBase targetNode,
            float timelineTime,
            StageTempoMap tempoMap,
            IReadOnlyList<LightNodeBase> children,
            IReadOnlyList<ILightState> prevStates,
            IReadOnlyList<ILightState> workingStates)
        {
            if (children == null || children.Count == 0 || workingStates == null || workingStates.Count < children.Count)
            {
                return;
            }

            IStageModuleMixer<TModule> mixer = StageTypeRegistry.GetMixer<TModule>();
            if (mixer == null)
            {
                return;
            }

            _activeClips.Clear();
            for (int i = 0; i < _clips.Count; i++)
            {
                LightClip<TModule> clip = _clips[i];
                if (clip != null && clip.Effect != null && clip.IsActiveAt(timelineTime))
                {
                    _activeClips.Add(clip);
                }
            }

            if (_activeClips.Count == 0)
            {
                return;
            }

            int count = children.Count;
            EnsureBuffers(count);

            if (_activeClips.Count == 1)
            {
                EvaluateSingleClip(
                    targetNode,
                    _activeClips[0],
                    timelineTime,
                    tempoMap,
                    children,
                    prevStates,
                    workingStates,
                    mixer,
                    count);
                return;
            }

            _activeClips.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
            EvaluateOverlappingClips(
                targetNode,
                timelineTime,
                tempoMap,
                children,
                prevStates,
                workingStates,
                mixer,
                count);
        }

        private void EvaluateSingleClip(
            LightNodeBase targetNode,
            LightClip<TModule> clip,
            float timelineTime,
            StageTempoMap tempoMap,
            IReadOnlyList<LightNodeBase> children,
            IReadOnlyList<ILightState> prevStates,
            IReadOnlyList<ILightState> workingStates,
            IStageModuleMixer<TModule> mixer,
            int count)
        {
            LightEffectContext context = BuildContext(targetNode, clip, timelineTime, tempoMap, children, prevStates);
            clip.Effect.Evaluate(context, _evalBuffer);

            float duration = Mathf.Max(0f, clip.Duration);
            float easeIn = Mathf.Max(0f, clip.EaseInDuration);
            float easeOut = Mathf.Max(0f, clip.EaseOutDuration);
            if (easeIn + easeOut > duration && easeIn + easeOut > 1e-6f)
            {
                float ratio = easeIn / (easeIn + easeOut);
                easeIn = duration * ratio;
                easeOut = duration * (1f - ratio);
            }

            float elapsed = timelineTime - clip.StartTime;
            float remaining = clip.EndTime - timelineTime;

            bool isEaseIn = easeIn > 1e-6f && elapsed < easeIn;
            bool isEaseOut = !isEaseIn && easeOut > 1e-6f && remaining < easeOut;
            float easeT = isEaseIn
                ? Mathf.Clamp01(elapsed / easeIn)
                : isEaseOut
                    ? Mathf.Clamp01(1f - remaining / easeOut)
                    : 1f;

            for (int i = 0; i < count; i++)
            {
                TModule baseModule = workingStates[i].GetModule<TModule>();
                TModule evaluated = _evalBuffer[i];
                TModule result = _mixBuffer[i];

                if (isEaseIn)
                {
                    mixer.EaseIn(baseModule, evaluated, easeT, result);
                }
                else if (isEaseOut)
                {
                    mixer.EaseOut(baseModule, evaluated, easeT, result);
                }
                else
                {
                    mixer.Blend(baseModule, evaluated, clip.Weight, clip.BlendMode, result);
                }

                workingStates[i].SetModule(result);
                _mixBuffer[i] = baseModule ?? CreateModuleInstance();
            }
        }

        private void EvaluateOverlappingClips(
            LightNodeBase targetNode,
            float timelineTime,
            StageTempoMap tempoMap,
            IReadOnlyList<LightNodeBase> children,
            IReadOnlyList<ILightState> prevStates,
            IReadOnlyList<ILightState> workingStates,
            IStageModuleMixer<TModule> mixer,
            int count)
        {
            LightClip<TModule> currentClip = _activeClips[0];
            currentClip.Effect.Evaluate(
                BuildContext(targetNode, currentClip, timelineTime, tempoMap, children, prevStates),
                _evalBuffer);

            for (int c = 1; c < _activeClips.Count; c++)
            {
                LightClip<TModule> nextClip = _activeClips[c];
                nextClip.Effect.Evaluate(
                    BuildContext(targetNode, nextClip, timelineTime, tempoMap, children, prevStates),
                    _secondaryEvalBuffer);

                float overlapDuration = Mathf.Max(1e-6f, currentClip.EndTime - nextClip.StartTime);
                float t = Mathf.Clamp01((timelineTime - nextClip.StartTime) / overlapDuration);

                for (int i = 0; i < count; i++)
                {
                    mixer.EaseIn(_evalBuffer[i], _secondaryEvalBuffer[i], t, _mixBuffer[i]);
                }

                TModule[] swap = _evalBuffer;
                _evalBuffer = _mixBuffer;
                _mixBuffer = swap;
                currentClip = nextClip;
            }

            for (int i = 0; i < count; i++)
            {
                TModule baseModule = workingStates[i].GetModule<TModule>();
                TModule result = _mixBuffer[i];
                mixer.Blend(baseModule, _evalBuffer[i], currentClip.Weight, currentClip.BlendMode, result);
                workingStates[i].SetModule(result);
                _mixBuffer[i] = baseModule ?? CreateModuleInstance();
            }
        }

        private static LightEffectContext BuildContext(
            LightNodeBase targetNode,
            LightClip<TModule> clip,
            float timelineTime,
            StageTempoMap tempoMap,
            IReadOnlyList<LightNodeBase> children,
            IReadOnlyList<ILightState> prevStates)
        {
            float elapsedTime = timelineTime - clip.StartTime;
            float duration = clip.Duration;
            float elapsedBeat = tempoMap != null
                ? tempoMap.TimeToBeat(timelineTime) - tempoMap.TimeToBeat(clip.StartTime)
                : 0f;
            float currentBpm = tempoMap != null ? tempoMap.GetBpmAt(timelineTime) : 120f;

            return new LightEffectContext(
                targetNode,
                children,
                prevStates,
                elapsedTime,
                duration,
                elapsedBeat,
                currentBpm);
        }

        private void EnsureBuffers(int count)
        {
            if (_evalBuffer != null && _evalBuffer.Length == count)
            {
                return;
            }

            _evalBuffer = AllocateModuleArray(count);
            _secondaryEvalBuffer = AllocateModuleArray(count);
            _mixBuffer = AllocateModuleArray(count);
        }

        private static TModule[] AllocateModuleArray(int count)
        {
            var array = new TModule[count];
            for (int i = 0; i < count; i++)
            {
                array[i] = CreateModuleInstance();
            }

            return array;
        }

        private static TModule CreateModuleInstance()
            => Activator.CreateInstance<TModule>();

        private static LightEffectBase<TModule> CloneEffect(LightEffectBase<TModule> source)
            => source?.Clone() as LightEffectBase<TModule>;
    }
}
