using Lumiscore.Timeline;
using UnityEditor;

namespace Lumiscore.Editor.Timeline
{
    [CustomEditor(typeof(StageClipAsset))]
    public class StageClipInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }
    }
}
