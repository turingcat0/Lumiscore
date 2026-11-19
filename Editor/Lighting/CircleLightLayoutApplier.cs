using Lumiscore.Lighting;
using UnityEditor;
using UnityEngine;

namespace Lumiscore.Editor.Lighting
{
    public class CircleLightLayoutApplier : ILightLayoutApplier<CircleLightLayout>
    {
        public void Apply(CircleLightLayout layout, LightGroupNode group)
        {
            if (layout == null || group == null)
            {
                return;
            }

            int count = group.Children.Count;
            if (count == 0)
            {
                return;
            }

            bool isFullCircle = Mathf.Abs(layout.ArcAngle) >= 360f;
            float step = 0f;
            if (isFullCircle)
            {
                step = layout.ArcAngle / count;
            }
            else if (count > 1)
            {
                step = layout.ArcAngle / (count - 1);
            }

            for (int i = 0; i < count; i++)
            {
                Transform target = group.Children[i].RootTransform;
                if (target == null)
                {
                    continue;
                }

                float angleDeg = layout.StartAngle + step * i;
                float rad = angleDeg * Mathf.Deg2Rad;
                Vector3 radial = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
                Vector3 position = radial * layout.Radius;
                Quaternion rotation = ResolveRotation(layout.Facing, radial);

                Undo.RecordObject(target, "Apply Circle Light Layout");
                target.localPosition = position;
                target.localRotation = rotation;
            }
        }

        private static Quaternion ResolveRotation(CircleLayoutFacing facing, Vector3 radial)
        {
            switch (facing)
            {
                case CircleLayoutFacing.Outward:
                    return Quaternion.LookRotation(radial, Vector3.up);
                case CircleLayoutFacing.Inward:
                    return Quaternion.LookRotation(-radial, Vector3.up);
                default:
                    return Quaternion.identity;
            }
        }
    }
}
