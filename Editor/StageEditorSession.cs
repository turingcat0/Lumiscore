using System;
using System.Collections.Generic;
using Lumiscore.Core;
using Lumiscore.Editor.Lighting;
using Lumiscore.Lighting;
using UnityEditor;
using UnityEngine.Playables;

namespace Lumiscore.Editor
{
    /// <summary>
    /// Editor-only session manager that tracks active EditMode previews, triggers non-serialized
    /// preview cleanup (<see cref="StageDriverBase.ResetPreview"/>) on exit/reload, and dispatches
    /// layout application — keeping runtime <see cref="StageController"/> free of editor state.
    /// </summary>
    [InitializeOnLoad]
    public static class StageEditorSession
    {
        private static readonly HashSet<StageController> s_ActivePreviews = new HashSet<StageController>();
        private static readonly Dictionary<PlayableDirector, StageController> s_DirectorMap =
            new Dictionary<PlayableDirector, StageController>();

        static StageEditorSession()
        {
            StageController.AnyPreviewStarted += HandlePreviewStarted;
            StageController.AnyPreviewStopped += HandlePreviewStopped;
            AssemblyReloadEvents.beforeAssemblyReload += ResetAll;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorApplication.quitting += ResetAll;
        }

        public static bool IsPreviewing(StageController controller)
            => controller != null && s_ActivePreviews.Contains(controller);

        public static void BeginPreview(StageController controller)
        {
            if (controller == null || !s_ActivePreviews.Add(controller))
            {
                return;
            }

            if (controller.TryGetComponent<PlayableDirector>(out PlayableDirector director))
            {
                s_DirectorMap[director] = controller;
                director.stopped -= HandleDirectorStopped;
                director.stopped += HandleDirectorStopped;
            }
        }

        public static void EndPreview(StageController controller)
        {
            if (controller == null || !s_ActivePreviews.Remove(controller))
            {
                return;
            }

            if (controller.TryGetComponent<PlayableDirector>(out PlayableDirector director))
            {
                director.stopped -= HandleDirectorStopped;
                s_DirectorMap.Remove(director);
            }

            controller.ResetAllPreviews();
        }

        public static void ResetAll()
        {
            if (s_ActivePreviews.Count == 0)
            {
                return;
            }

            var controllers = new List<StageController>(s_ActivePreviews);
            for (int i = 0; i < controllers.Count; i++)
            {
                EndPreview(controllers[i]);
            }

            s_ActivePreviews.Clear();
            s_DirectorMap.Clear();
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

        private static void HandleDirectorStopped(PlayableDirector director)
        {
            if (director != null && s_DirectorMap.TryGetValue(director, out StageController controller))
            {
                EndPreview(controller);
            }
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode)
            {
                ResetAll();
            }
        }
    }
}
