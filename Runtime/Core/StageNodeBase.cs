using System;

namespace Lumiscore.Core
{
    public abstract class StageNodeBase : IStageNode
    {
        public string Id { get; }
        public string Name { get; set; }

        protected StageNodeBase(string name = null, string id = null)
        {
            Id = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString("N") : id;
            Name = name ?? string.Empty;
        }
    }
}
