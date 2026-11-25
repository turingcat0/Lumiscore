using System;
using UnityEngine;

namespace Lumiscore.Core
{
    /// <summary>
    /// Non-generic serializable base class for stage drivers embedded under <c>[SerializeReference]</c>
    /// on scene fixture components (not a <c>MonoBehaviour</c>).
    /// </summary>
    [Serializable]
    public abstract class StageDriverBase
    {
        public abstract Type StateType { get; }

        public void ApplyState(IStageState state) => ApplyStateCore(state);

        /// <summary>
        /// Registers serialized scene properties modified during EditMode preview so Unity Timeline
        /// can back up and restore them losslessly. Defaults to registering child Transforms and Lights;
        /// override for custom fixture components or finer control.
        /// </summary>
        public virtual void GatherPreviewProperties(GameObject fixtureRoot, IStagePropertyCollector collector)
        {
            if (fixtureRoot == null || collector == null)
            {
                return;
            }

            collector.AddTransformsInChildren(fixtureRoot);
            Light[] lights = fixtureRoot.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < lights.Length; i++)
            {
                collector.AddComponent(lights[i]);
            }
        }

        /// <summary>
        /// Optional hook invoked when EditMode preview ends to clean up non-serialized runtime render state
        /// (such as <see cref="MaterialPropertyBlock"/> overrides).
        /// </summary>
        public virtual void ResetPreview(GameObject fixtureRoot)
        {
        }

        protected abstract void ApplyStateCore(IStageState state);
    }
}
