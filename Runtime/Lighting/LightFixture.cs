using System;
using Lumiscore.Core;
using UnityEngine;

namespace Lumiscore.Lighting
{
    /// <summary>
    /// Scene-side fixture component attached to a physical light <see cref="GameObject"/>.
    /// Encapsulates scene-specific fixture coupling (embedded <see cref="StageDriverBase"/>,
    /// state type, and future fixture metadata), keeping asset-side <see cref="LightLeafNode"/>
    /// completely scene-agnostic.
    /// </summary>
    public class LightFixture : MonoBehaviour
    {
        [SerializeField] private string _nodeId;
        [SerializeReference] private StageDriverBase _driver;

        public string NodeId
        {
            get
            {
                EnsureNodeId();
                return _nodeId;
            }
            set => _nodeId = value ?? string.Empty;
        }

        public StageDriverBase Driver
        {
            get => _driver;
            set => _driver = value;
        }

        public Type StateType => _driver?.StateType;

        public void EnsureNodeId()
        {
            if (string.IsNullOrEmpty(_nodeId))
            {
                _nodeId = Guid.NewGuid().ToString("N");
            }
        }

        private void Reset()
        {
            EnsureNodeId();
        }
    }
}
