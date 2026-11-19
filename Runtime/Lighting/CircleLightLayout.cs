namespace Lumiscore.Lighting
{
    public enum CircleLayoutFacing
    {
        KeepForward,
        Outward,
        Inward,
    }

    public class CircleLightLayout : ILightLayout
    {
        public float Radius { get; set; } = 3f;
        public float StartAngle { get; set; } = 0f;
        public float ArcAngle { get; set; } = 360f;
        public CircleLayoutFacing Facing { get; set; } = CircleLayoutFacing.Outward;
    }
}
