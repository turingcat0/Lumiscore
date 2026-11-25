using System;
using UnityEngine;

namespace Lumiscore.Lighting
{
    /// <summary>
    /// Scene-side lighting group component attached to a stage hierarchy <see cref="GameObject"/>.
    /// Owns a stable <see cref="NodeId"/> and editable <see cref="ILightLayout"/> for scene authoring.
    /// </summary>
    public class LightGroup : MonoBehaviour
    {
        [SerializeField] private string _nodeId;
        [SerializeReference] private ILightLayout _layout;

        public string NodeId
        {
            get
            {
                EnsureNodeId();
                return _nodeId;
            }
            set => _nodeId = value ?? string.Empty;
        }

        public ILightLayout Layout
        {
            get => _layout;
            set => _layout = value;
        }

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
