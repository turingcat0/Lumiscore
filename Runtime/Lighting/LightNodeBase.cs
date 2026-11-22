using System;
using Lumiscore.Core;
using UnityEngine;

namespace Lumiscore.Lighting
{
    [Serializable]
    public abstract class LightNodeBase : StageNodeBase, ILightNode
    {
        [NonSerialized] private Transform _rootTransform;

        public Transform RootTransform
        {
            get => _rootTransform;
            set => _rootTransform = value;
        }

        protected LightNodeBase()
        {
        }

        protected LightNodeBase(string name, string id = null, Transform rootTransform = null)
            : base(name, id)
        {
            _rootTransform = rootTransform;
        }
    }
}
