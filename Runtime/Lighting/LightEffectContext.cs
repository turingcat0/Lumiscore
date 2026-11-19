namespace Lumiscore.Lighting
{
    public struct LightEffectContext<TModule>
        where TModule : class, ILightStateModule
    {
        public ILightNode TargetNode;
        public TModule Parent;
        public TModule Enter;
        public ILightState PrevState;
        public float ElapsedTime;

        public LightEffectContext(
            ILightNode targetNode,
            TModule parent,
            TModule enter,
            ILightState prevState,
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
