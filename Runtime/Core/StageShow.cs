using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lumiscore.Core
{
    /// <summary>
    /// Root authoring and runtime data model for a single show/song.
    /// Serializes tracks through private non-generic DTOs to satisfy Unity's <c>[SerializeReference]</c>
    /// constraints while exposing strongly-typed tracks and an O(1) node-to-tracks index in memory.
    /// </summary>
    [Serializable]
    public class StageShow : ISerializationCallbackReceiver
    {
        [Serializable]
        private class SerializedTrackData
        {
            public string Id;
            public string TrackTypeName;
            public string ModuleTypeName;
            [SerializeReference] public IStageNode TargetNode;
            public List<SerializedClipData> Clips = new List<SerializedClipData>();
        }

        [Serializable]
        private class SerializedClipData
        {
            public string Id;
            public float StartTime;
            public float EndTime;
            public StageBlendMode BlendMode;
            public float Weight;
            public float EaseInDuration;
            public float EaseOutDuration;
            [SerializeReference] public StageEffectBase Effect;
        }

        [SerializeField] private string _id;
        [SerializeField] private string _name;
        [SerializeField] private StageTempoMap _tempoMap = new StageTempoMap();
        [SerializeReference] private List<IStageNode> _rootNodes = new List<IStageNode>();
        [SerializeField] private List<SerializedTrackData> _serializedTracks = new List<SerializedTrackData>();

        [NonSerialized] private List<IStageTrack> _tracks = new List<IStageTrack>();
        [NonSerialized] private Dictionary<IStageNode, List<IStageTrack>> _tracksByNode =
            new Dictionary<IStageNode, List<IStageTrack>>();

        public string Id => _id;

        public string Name
        {
            get => _name;
            set => _name = value ?? string.Empty;
        }

        public StageTempoMap TempoMap => _tempoMap ?? (_tempoMap = new StageTempoMap());
        public IReadOnlyList<IStageNode> RootNodes => _rootNodes;
        public IReadOnlyList<IStageTrack> Tracks => _tracks;

        public StageShow()
            : this(null, null)
        {
        }

        public StageShow(string name, string id = null)
        {
            _id = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString("N") : id;
            _name = name ?? string.Empty;
        }

        public void AddRootNode(IStageNode node)
        {
            if (node != null)
            {
                _rootNodes.Add(node);
            }
        }

        public bool RemoveRootNode(IStageNode node)
        {
            return node != null && _rootNodes.Remove(node);
        }

        public void AddTrack(IStageTrack track)
        {
            if (track == null)
            {
                return;
            }

            _tracks.Add(track);
            IndexTrack(track);
        }

        public bool RemoveTrack(IStageTrack track)
        {
            if (track == null || !_tracks.Remove(track))
            {
                return false;
            }

            if (track.TargetNode != null &&
                _tracksByNode != null &&
                _tracksByNode.TryGetValue(track.TargetNode, out List<IStageTrack> nodeTracks))
            {
                nodeTracks.Remove(track);
            }

            return true;
        }

        public IReadOnlyList<IStageTrack> GetTracks(IStageNode node)
        {
            if (node != null &&
                _tracksByNode != null &&
                _tracksByNode.TryGetValue(node, out List<IStageTrack> nodeTracks))
            {
                return nodeTracks;
            }

            return Array.Empty<IStageTrack>();
        }

        private void IndexTrack(IStageTrack track)
        {
            if (track == null || track.TargetNode == null)
            {
                return;
            }

            if (_tracksByNode == null)
            {
                _tracksByNode = new Dictionary<IStageNode, List<IStageTrack>>();
            }

            if (!_tracksByNode.TryGetValue(track.TargetNode, out List<IStageTrack> nodeTracks))
            {
                nodeTracks = new List<IStageTrack>();
                _tracksByNode[track.TargetNode] = nodeTracks;
            }

            nodeTracks.Add(track);
        }

        public IStageTrack FindTrack(string trackId)
        {
            if (string.IsNullOrEmpty(trackId))
            {
                return null;
            }

            for (int i = 0; i < _tracks.Count; i++)
            {
                if (_tracks[i] != null && _tracks[i].Id == trackId)
                {
                    return _tracks[i];
                }
            }

            return null;
        }

        public IStageTrack FindTrackContainingClip(string clipId)
        {
            if (string.IsNullOrEmpty(clipId))
            {
                return null;
            }

            for (int i = 0; i < _tracks.Count; i++)
            {
                IStageTrack track = _tracks[i];
                if (track != null && track.FindClip(clipId) != null)
                {
                    return track;
                }
            }

            return null;
        }

        public IStageClip FindClip(string clipId)
        {
            IStageTrack track = FindTrackContainingClip(clipId);
            return track?.FindClip(clipId);
        }

        public bool TryGetEffectPropertyPath(string trackId, string clipId, out string relativePath)
        {
            relativePath = null;
            if (string.IsNullOrEmpty(trackId) || string.IsNullOrEmpty(clipId) || _tracks == null)
            {
                return false;
            }

            for (int t = 0; t < _tracks.Count; t++)
            {
                IStageTrack track = _tracks[t];
                if (track == null || track.Id != trackId)
                {
                    continue;
                }

                IReadOnlyList<IStageClip> clips = track.Clips;
                for (int c = 0; c < clips.Count; c++)
                {
                    if (clips[c] != null && clips[c].Id == clipId)
                    {
                        relativePath = $"_serializedTracks.Array.data[{t}].Clips.Array.data[{c}].Effect";
                        return true;
                    }
                }
            }

            return false;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (_tracks == null)
            {
                return;
            }

            _serializedTracks.Clear();

            for (int i = 0; i < _tracks.Count; i++)
            {
                IStageTrack track = _tracks[i];
                if (track == null)
                {
                    continue;
                }

                Type trackType = track.GetType();
                var trackData = new SerializedTrackData
                {
                    Id = track.Id,
                    TrackTypeName = ToTypeName(trackType.IsGenericType ? trackType.GetGenericTypeDefinition() : trackType),
                    ModuleTypeName = ToTypeName(track.ModuleType),
                    TargetNode = track.TargetNode
                };

                IReadOnlyList<IStageClip> clips = track.Clips;
                for (int c = 0; c < clips.Count; c++)
                {
                    IStageClip clip = clips[c];
                    if (clip == null)
                    {
                        continue;
                    }

                    trackData.Clips.Add(new SerializedClipData
                    {
                        Id = clip.Id,
                        StartTime = clip.StartTime,
                        EndTime = clip.EndTime,
                        BlendMode = clip.BlendMode,
                        Weight = clip.Weight,
                        EaseInDuration = clip.EaseInDuration,
                        EaseOutDuration = clip.EaseOutDuration,
                        Effect = clip.Effect
                    });
                }

                _serializedTracks.Add(trackData);
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            _tracks = new List<IStageTrack>();
            _tracksByNode = new Dictionary<IStageNode, List<IStageTrack>>();

            if (_serializedTracks == null)
            {
                return;
            }

            for (int i = 0; i < _serializedTracks.Count; i++)
            {
                SerializedTrackData trackData = _serializedTracks[i];
                if (trackData == null)
                {
                    continue;
                }

                IStageTrack track = InstantiateTrack(trackData.TrackTypeName, trackData.ModuleTypeName);
                if (track == null)
                {
                    continue;
                }

                track.Restore(trackData.Id, trackData.TargetNode);

                if (trackData.Clips != null)
                {
                    for (int c = 0; c < trackData.Clips.Count; c++)
                    {
                        SerializedClipData clipData = trackData.Clips[c];
                        if (clipData == null)
                        {
                            continue;
                        }

                        IStageClip clip = track.CreateClip(clipData.StartTime, clipData.EndTime, clipData.Id);
                        clip.BlendMode = clipData.BlendMode;
                        clip.Weight = clipData.Weight;
                        clip.EaseInDuration = clipData.EaseInDuration;
                        clip.EaseOutDuration = clipData.EaseOutDuration;
                        clip.TrySetEffect(clipData.Effect);
                    }
                }

                _tracks.Add(track);
                IndexTrack(track);
            }
        }

        private static IStageTrack InstantiateTrack(string trackTypeName, string moduleTypeName)
        {
            if (string.IsNullOrEmpty(trackTypeName))
            {
                return null;
            }

            Type trackType = Type.GetType(trackTypeName);
            if (trackType == null)
            {
                return null;
            }

            if (trackType.IsGenericTypeDefinition)
            {
                if (string.IsNullOrEmpty(moduleTypeName))
                {
                    return null;
                }

                Type moduleType = Type.GetType(moduleTypeName);
                if (moduleType == null)
                {
                    return null;
                }

                trackType = trackType.MakeGenericType(moduleType);
            }

            return Activator.CreateInstance(trackType) as IStageTrack;
        }

        private static string ToTypeName(Type type)
        {
            return type == null ? string.Empty : $"{type.FullName}, {type.Assembly.GetName().Name}";
        }
    }
}
