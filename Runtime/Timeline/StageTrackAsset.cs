using System;
using Lumiscore.Core;
using Lumiscore.Lighting;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Lumiscore.Timeline
{
    [TrackClipType(typeof(StageClipAsset))]
    public class StageTrackAsset : TrackAsset
    {
        [SerializeField] private StageShowAsset _showAsset;
        [SerializeField] private string _trackId;
        [SerializeField] private string _moduleTypeName;

        public StageShowAsset ShowAsset
        {
            get => _showAsset;
            set => _showAsset = value;
        }

        public string TrackId
        {
            get => _trackId;
            set => _trackId = value;
        }

        public string ModuleTypeName
        {
            get => _moduleTypeName;
            set => _moduleTypeName = value;
        }

        public Type ResolveModuleType()
            => string.IsNullOrEmpty(_moduleTypeName) ? null : Type.GetType(_moduleTypeName);

        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            var playable = ScriptPlayable<StageTrackMixer>.Create(graph, inputCount);
            StageTrackMixer mixer = playable.GetBehaviour();
            mixer.ShowAsset = _showAsset;
            mixer.TrackId = _trackId;
            mixer.Controller = go != null ? go.GetComponent<StageController>() : null;
            return playable;
        }
    }
}
