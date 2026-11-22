using System;
using UnityEngine;

namespace Lumiscore.Lighting
{
    [Serializable]
    public class LineLightLayout : ILightLayout
    {
        [SerializeField] private float _spacing = 1f;
        [SerializeField] private Vector3 _direction = Vector3.right;
        [SerializeField] private bool _centered = true;

        public float Spacing
        {
            get => _spacing;
            set => _spacing = value;
        }

        public Vector3 Direction
        {
            get => _direction;
            set => _direction = value;
        }

        public bool Centered
        {
            get => _centered;
            set => _centered = value;
        }
    }
}
