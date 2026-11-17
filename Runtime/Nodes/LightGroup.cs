using System.Collections.Generic;

namespace Lumiscore.Nodes
{
    public class LightGroup : ILightNode
    {
        public List<ILightNode> Children { get; } = new List<ILightNode>();

        public LightGroup()
        {
        }

        public LightGroup(IEnumerable<ILightNode> children)
        {
            if (children != null)
            {
                Children.AddRange(children);
            }
        }
    }
}
