using System;
using UnityEngine;

namespace Lumiscore.Lighting
{
    [Serializable]
    public class LightLeafNode : LightNodeBase
    {
        public LightLeafNode()
        {
        }

        public LightLeafNode(string name, string id = null, Transform rootTransform = null)
            : base(name, id, rootTransform)
        {
        }
    }
}
