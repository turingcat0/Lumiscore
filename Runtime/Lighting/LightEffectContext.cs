namespace Lumiscore.Lighting
{
    public struct LightEffectContext
    {
        public ILightNode TargetNode;
        public ILightState PrevState;
        public float ElapsedTime;

        public LightEffectContext(
            ILightNode targetNode,
            ILightState prevState,
            float elapsedTime)
        {
            TargetNode = targetNode;
            PrevState = prevState;
            ElapsedTime = elapsedTime;
        }
    }
}
