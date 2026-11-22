using System;
using UnityEngine;

namespace Lumiscore.Core
{
    [Serializable]
    public abstract class StageNodeBase : IStageNode
    {
        [SerializeField] private string _id;
        [SerializeField] private string _name;

        public string Id => _id;

        public string Name
        {
            get => _name;
            set => _name = value ?? string.Empty;
        }

        protected StageNodeBase()
            : this(null, null)
        {
        }

        protected StageNodeBase(string name, string id = null)
        {
            _id = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString("N") : id;
            _name = name ?? string.Empty;
        }
    }
}
