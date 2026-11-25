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
        private sealed class TimelinePropertyCollectorAdapter : IStagePropertyCollector
        {
            private readonly IPropertyCollector _inner;

            public TimelinePropertyCollectorAdapter(IPropertyCollector inner)
            {
                _inner = inner;
            }

            public void AddComponent(Component component)
            {
                if (component != null)
                {
                    _inner.AddFromComponent(component.gameObject, component);
                }
            }

            public void AddProperty<TComponent>(GameObject target, string propertyName)
                where TComponent : Component
            {
                if (target != null && !string.IsNullOrEmpty(propertyName))
                {
                    _inner.AddFromName<TComponent>(target, propertyName);
                }
            }

            public void AddTransformsInChildren(GameObject root)
            {
                if (root == null)
                {
                    return;
                }

                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < transforms.Length; i++)
                {
                    _inner.AddFromComponent(transforms[i].gameObject, transforms[i]);
                }
            }
        }

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

        public override void GatherProperties(PlayableDirector director, IPropertyCollector driver)
        {
            base.GatherProperties(director, driver);
            if (director == null || driver == null)
            {
                return;
            }

            StageController controller = director.GetComponent<StageController>();
            if (controller == null)
            {
                return;
            }

            controller.GatherAllPreviewProperties(new TimelinePropertyCollectorAdapter(driver));
        }
    }
}
