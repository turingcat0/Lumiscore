using UnityEngine;

namespace Lumiscore.Lighting
{
    public class LineLightLayout : ILightLayout
    {
        public float Spacing { get; set; } = 1f;
        public Vector3 Direction { get; set; } = Vector3.right;
        public bool Centered { get; set; } = true;
    }
}
