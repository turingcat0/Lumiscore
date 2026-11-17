using Lumiscore.Nodes;
using Lumiscore.State;

namespace Lumiscore.Patterns
{
    public struct PatternContext<TModule>
        where TModule : class, IFixtureStateModule
    {
        public ILightNode TargetNode;
        public TModule Parent;
        public TModule Enter;
        public IFixtureState PrevState;
        public float ElapsedTime;

        public PatternContext(
            ILightNode targetNode,
            TModule parent,
            TModule enter,
            IFixtureState prevState,
            float elapsedTime)
        {
            TargetNode = targetNode;
            Parent = parent;
            Enter = enter;
            PrevState = prevState;
            ElapsedTime = elapsedTime;
        }
    }
}
