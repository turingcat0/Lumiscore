using Lumiscore.Lighting;
using UnityEditor;
using UnityEngine;

namespace Lumiscore.Editor.Lighting
{
    public class GridLightLayoutApplier : ILightLayoutApplier<GridLightLayout>
    {
        public void Apply(GridLightLayout layout, LightNodeBase group)
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

            int cols = Mathf.Max(1, layout.Columns);
            int rows = (count + cols - 1) / cols;
            float originX = layout.Centered ? -0.5f * (cols - 1) * layout.SpacingX : 0f;
            float originZ = layout.Centered ? -0.5f * (rows - 1) * layout.SpacingY : 0f;

            for (int i = 0; i < count; i++)
            {
                Transform target = group.Children[i].RootTransform;
                if (target == null)
                {
                    continue;
                }

                (int row, int col) = layout.GetGridCoord(i);
                Vector3 position = new Vector3(
                    originX + col * layout.SpacingX,
                    0f,
                    originZ + row * layout.SpacingY);

                Undo.RecordObject(target, "Apply Grid Light Layout");
                target.localPosition = position;
                target.localRotation = Quaternion.identity;
            }
        }
    }
}
