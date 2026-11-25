using System;
using UnityEngine;

namespace Lumiscore.Lighting
{
    /// <summary>
    /// Terminal fixture leaf node with no children. Receives the final evaluated <see cref="ILightState"/>
    /// and forwards it to its runtime-injected <see cref="LightFixture"/>.
    /// </summary>
    [Serializable]
    public class LightLeafNode : LightNodeBase
    {
        [NonSerialized] private LightFixture _fixture;

        public LightFixture Fixture
        {
            get => _fixture;
            set => _fixture = value;
        }

        public LightLeafNode()
        {
        }

        public LightLeafNode(string name, string id = null, Transform rootTransform = null)
            : base(name, id, rootTransform)
        {
        }
    }
}
