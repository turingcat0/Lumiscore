using System;
using System.Collections.Generic;
using Lumiscore.Core;
using Lumiscore.Editor.Lighting;
using Lumiscore.Lighting;
using UnityEditor;

namespace Lumiscore.Editor
{
    [InitializeOnLoad]
    public static class StageEditorSession
    {
        private static readonly Dictionary<StageController, Dictionary<string, IStageState>> s_CapturedSnapshots =
            new Dictionary<StageController, Dictionary<string, IStageState>>();

        static StageEditorSession()
        {
            StageController.AnyPreviewStarted += HandlePreviewStarted;
            StageController.AnyPreviewStopped += HandlePreviewStopped;
            AssemblyReloadEvents.beforeAssemblyReload += RestoreAll;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorApplication.quitting += RestoreAll;
        }

        public static bool IsPreviewing(StageController controller)
            => controller != null && s_CapturedSnapshots.ContainsKey(controller);

        public static void BeginPreview(StageController controller)
        {
            if (controller == null || s_CapturedSnapshots.ContainsKey(controller))
            {
                return;
            }

            var snapshot = new Dictionary<string, IStageState>();
            controller.CaptureAllBoundStates(snapshot);
            s_CapturedSnapshots[controller] = snapshot;
        }

        public static void EndPreview(StageController controller)
        {
            if (controller == null || !s_CapturedSnapshots.TryGetValue(controller, out Dictionary<string, IStageState> snapshot))
            {
                return;
            }

            s_CapturedSnapshots.Remove(controller);
            controller.ApplyCapturedStates(snapshot);
        }

        public static void RestoreAll()
        {
            if (s_CapturedSnapshots.Count == 0)
            {
                return;
            }

            var controllers = new List<StageController>(s_CapturedSnapshots.Keys);
            for (int i = 0; i < controllers.Count; i++)
            {
                EndPreview(controllers[i]);
            }

            s_CapturedSnapshots.Clear();
        }

        public static bool ApplyLayout(LightNodeBase node)
        {
            if (node == null || node.Layout == null)
            {
                return false;
            }

            Type layoutType = node.Layout.GetType();
            Type applierInterface = typeof(ILightLayoutApplier<>).MakeGenericType(layoutType);
            IReadOnlyList<Type> candidates = StageTypeRegistry.GetConcreteTypesDerivedFrom(applierInterface);
            if (candidates.Count == 0)
            {
                return false;
            }

            object applier = Activator.CreateInstance(candidates[0]);
            applierInterface.GetMethod("Apply")?.Invoke(applier, new object[] { node.Layout, node });
            return true;
        }

        private static void HandlePreviewStarted(StageController controller)
        {
            if (!EditorApplication.isPlaying)
            {
                BeginPreview(controller);
            }
        }

        private static void HandlePreviewStopped(StageController controller)
        {
            if (!EditorApplication.isPlaying)
            {
                EndPreview(controller);
            }
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode)
            {
                RestoreAll();
            }
        }
    }
}
