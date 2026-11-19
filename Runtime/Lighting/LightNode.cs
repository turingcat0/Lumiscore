using UnityEngine;

namespace Lumiscore.Lighting
{
    public class LightNode : LightNodeBase
    {
        public LightNode(string name = null, string id = null, Transform rootTransform = null)
            : base(name, id, rootTransform)
        {
        }
    }
}
