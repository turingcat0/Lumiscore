using System;
using System.Collections.Generic;
using Lumiscore.Core;
using UnityEngine;

namespace Lumiscore.Lighting
{
    /// <summary>
    /// Base class for all lighting hierarchy nodes inside a <see cref="StageShow"/>.
    /// Intentionally excludes fixture state type metadata; scene bindings own node-to-state mapping.
    /// </summary>
    [Serializable]
    public abstract class LightNodeBase : StageNodeBase
    {
        [NonSerialized] private Transform _rootTransform;
        [NonSerialized] private int _index;
        [NonSerialized] private int _count = 1;

        public Transform RootTransform
        {
            get => _rootTransform;
            set => _rootTransform = value;
        }

        public int Index => _index;

        public int Count => _count;

        public virtual IReadOnlyList<LightNodeBase> Children => Array.Empty<LightNodeBase>();

        public virtual ILightLayout Layout
        {
            get => null;
            set { }
        }

        protected LightNodeBase()
        {
        }

        protected LightNodeBase(string name, string id = null, Transform rootTransform = null)
            : base(name, id)
        {
            _rootTransform = rootTransform;
        }

        internal void SetLayoutMetrics(int index, int count)
        {
            _index = index;
            _count = count;
        }
    }
}
