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
        [SerializeReference] private ILightNode _targetNode;
        [SerializeField] private List<LightClip<TModule>> _clips = new List<LightClip<TModule>>();

        public string Id => _id;

        public ILightNode TargetNode
        {
            get => _targetNode;
            set => _targetNode = value;
        }

        IStageNode IStageTrack.TargetNode => _targetNode;

        public Type ModuleType => typeof(TModule);

        public List<LightClip<TModule>> Clips => _clips;

        IReadOnlyList<IStageClip> IStageTrack.Clips => _clips;

        public LightTrack()
            : this(null, null)
        {
        }

        public LightTrack(ILightNode targetNode, string id = null)
        {
            _id = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString("N") : id;
            _targetNode = targetNode;
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
                EaseDuration = sourceClip.EaseDuration,
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

        public void Restore(string id, IStageNode targetNode)
        {
            _id = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString("N") : id;
            _targetNode = targetNode as ILightNode;
        }

        public void EvaluateTrack(float timelineTime, ILightState prevState)
        {
        }

        private static ILightEffect<TModule> CloneEffect(ILightEffect<TModule> source)
        {
            if (source == null)
            {
                return null;
            }

            Type effectType = source.GetType();
            string json = JsonUtility.ToJson(source);
            return JsonUtility.FromJson(json, effectType) as ILightEffect<TModule>;
        }
    }
}
