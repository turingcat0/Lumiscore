using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Lumiscore.Timeline
{
    [Serializable]
    public class StageClipAsset : PlayableAsset, ITimelineClipAsset
    {
        [SerializeField] private string _clipId;

        public string ClipId
        {
            get => _clipId;
            set => _clipId = value;
        }

        public ClipCaps clipCaps => ClipCaps.Blending;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<StageClipBehaviour>.Create(graph);
            playable.GetBehaviour().ClipId = _clipId;
            return playable;
        }
    }
}
