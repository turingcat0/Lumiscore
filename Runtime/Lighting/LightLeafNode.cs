using System;
using UnityEngine;

namespace Lumiscore.Lighting
{
    /// <summary>
    /// Terminal fixture leaf node with no children. Receives the final evaluated <see cref="ILightState"/>
    /// and forwards it to its bound scene fixture/driver.
    /// </summary>
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
