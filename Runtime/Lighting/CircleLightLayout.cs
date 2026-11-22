using System;
using UnityEngine;

namespace Lumiscore.Lighting
{
    public enum CircleLayoutFacing
    {
        KeepForward,
        Outward,
        Inward,
    }

    [Serializable]
    public class CircleLightLayout : ILightLayout
    {
        [SerializeField] private float _radius = 3f;
        [SerializeField] private float _startAngle;
        [SerializeField] private float _arcAngle = 360f;
        [SerializeField] private CircleLayoutFacing _facing = CircleLayoutFacing.Outward;

        public float Radius
        {
            get => _radius;
            set => _radius = value;
        }

        public float StartAngle
        {
            get => _startAngle;
            set => _startAngle = value;
        }

        public float ArcAngle
        {
            get => _arcAngle;
            set => _arcAngle = value;
        }

        public CircleLayoutFacing Facing
        {
            get => _facing;
            set => _facing = value;
        }
    }
}
