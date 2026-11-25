using System;
using Lumiscore.Core;
using UnityEngine;

namespace Lumiscore.Lighting
{
    [Serializable]
    public abstract class LightNodeBase : StageNodeBase, ILightNode
    {
        [SerializeField] private string _stateTypeName;
        [NonSerialized] private Transform _rootTransform;

        public string StateTypeName
        {
            get => _stateTypeName;
            set => _stateTypeName = value ?? string.Empty;
        }

        public Transform RootTransform
        {
            get => _rootTransform;
            set => _rootTransform = value;
        }

        public Type ResolveStateType()
            => string.IsNullOrEmpty(_stateTypeName) ? null : Type.GetType(_stateTypeName);

        public void SetStateType(Type stateType)
            => _stateTypeName = stateType != null ? stateType.AssemblyQualifiedName : string.Empty;

        protected LightNodeBase()
        {
        }

        protected LightNodeBase(string name, string id = null, Transform rootTransform = null)
            : base(name, id)
        {
            _rootTransform = rootTransform;
        }
    }
}
