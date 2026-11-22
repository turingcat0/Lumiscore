using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lumiscore.Lighting
{
    [Serializable]
    public class LightGroupNode : LightNodeBase
    {
        [SerializeReference] private ILightLayout _layout;
        [SerializeReference] private List<ILightNode> _children = new List<ILightNode>();

        public ILightLayout Layout
        {
            get => _layout;
            set => _layout = value;
        }

        public IReadOnlyList<ILightNode> Children => _children;

        public LightGroupNode()
        {
        }

        public LightGroupNode(string name, string id = null, Transform rootTransform = null)
            : base(name, id, rootTransform)
        {
        }

        public LightGroupNode(
            IEnumerable<ILightNode> children,
            string name = null,
            string id = null,
            Transform rootTransform = null)
            : base(name, id, rootTransform)
        {
            if (children != null)
            {
                foreach (ILightNode child in children)
                {
                    AddChild(child);
                }
            }
        }

        public void AddChild(ILightNode child)
        {
            if (child != null)
            {
                _children.Add(child);
            }
        }

        public bool RemoveChild(ILightNode child)
        {
            return child != null && _children.Remove(child);
        }
    }
}
