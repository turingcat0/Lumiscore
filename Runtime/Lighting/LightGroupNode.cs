using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lumiscore.Lighting
{
    [Serializable]
    public class LightGroupNode : LightNodeBase, ISerializationCallbackReceiver
    {
        [SerializeReference] private ILightLayout _layout;
        [SerializeReference] private List<LightNodeBase> _children = new List<LightNodeBase>();

        public override IReadOnlyList<LightNodeBase> Children => _children;

        public override ILightLayout Layout
        {
            get => _layout;
            set => _layout = value;
        }

        public LightGroupNode()
        {
        }

        public LightGroupNode(string name, string id = null, Transform rootTransform = null)
            : base(name, id, rootTransform)
        {
        }

        public LightGroupNode(
            IEnumerable<LightNodeBase> children,
            string name = null,
            string id = null,
            Transform rootTransform = null)
            : base(name, id, rootTransform)
        {
            if (children != null)
            {
                foreach (LightNodeBase child in children)
                {
                    if (child != null)
                    {
                        _children.Add(child);
                    }
                }

                RefreshChildLayoutMetrics();
            }
        }

        public void AddChild(LightNodeBase child)
        {
            if (child == null)
            {
                return;
            }

            _children.Add(child);
            RefreshChildLayoutMetrics();
        }

        public bool RemoveChild(LightNodeBase child)
        {
            if (child == null || !_children.Remove(child))
            {
                return false;
            }

            RefreshChildLayoutMetrics();
            return true;
        }

        public void RefreshChildLayoutMetrics()
        {
            int count = _children.Count;
            for (int i = 0; i < count; i++)
            {
                _children[i]?.SetLayoutMetrics(i, count);
            }
        }

        public static LightGroupNode WrapSingleLight(LightLeafNode leaf, string wrapperName = null)
        {
            if (leaf == null)
            {
                return null;
            }

            return new LightGroupNode(
                new LightNodeBase[] { leaf },
                string.IsNullOrEmpty(wrapperName) ? leaf.Name : wrapperName,
                null,
                leaf.RootTransform);
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (_children == null)
            {
                _children = new List<LightNodeBase>();
            }

            RefreshChildLayoutMetrics();
        }
    }
}
