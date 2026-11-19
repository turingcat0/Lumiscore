using Lumiscore.Core;
using UnityEngine;

namespace Lumiscore.Lighting
{
    public abstract class LightNodeBase : StageNodeBase, ILightNode
    {
        public Transform RootTransform { get; set; }

        protected LightNodeBase(string name = null, string id = null, Transform rootTransform = null)
            : base(name, id)
        {
            RootTransform = rootTransform;
        }
    }
}
