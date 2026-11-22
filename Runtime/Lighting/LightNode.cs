using System;
using UnityEngine;

namespace Lumiscore.Lighting
{
    [Serializable]
    public class LightNode : LightNodeBase
    {
        public LightNode()
        {
        }

        public LightNode(string name, string id = null, Transform rootTransform = null)
            : base(name, id, rootTransform)
        {
        }
    }
}
