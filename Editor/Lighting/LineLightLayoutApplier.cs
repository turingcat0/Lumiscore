using Lumiscore.Lighting;
using UnityEditor;
using UnityEngine;

namespace Lumiscore.Editor.Lighting
{
    public class LineLightLayoutApplier : ILightLayoutApplier<LineLightLayout>
    {
        public void Apply(LineLightLayout layout, LightGroupNode group)
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

            Vector3 direction = layout.Direction.sqrMagnitude > 0.000001f
                ? layout.Direction.normalized
                : Vector3.right;
            float startOffset = layout.Centered ? -0.5f * (count - 1) * layout.Spacing : 0f;

            for (int i = 0; i < count; i++)
            {
                Transform target = group.Children[i].RootTransform;
                if (target == null)
                {
                    continue;
                }

                Vector3 position = direction * (startOffset + i * layout.Spacing);
                Undo.RecordObject(target, "Apply Line Light Layout");
                target.localPosition = position;
                target.localRotation = Quaternion.identity;
            }
        }
    }
}
