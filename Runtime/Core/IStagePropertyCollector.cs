using UnityEngine;

namespace Lumiscore.Core
{
    /// <summary>
    /// Lighting- and Timeline-agnostic property collector used by <see cref="StageDriverBase.GatherPreviewProperties"/>
    /// to register serialized scene properties for automatic lossless preview restoration.
    /// </summary>
    public interface IStagePropertyCollector
    {
        void AddComponent(Component component);
        void AddProperty<TComponent>(GameObject target, string propertyName) where TComponent : Component;
        void AddTransformsInChildren(GameObject root);
    }
}
