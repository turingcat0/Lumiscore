using Lumiscore.Core;

namespace Lumiscore.Lighting
{
    public interface ILightEffect<TModule> : IStageEffect
        where TModule : class, ILightStateModule
    {
        TModule Evaluate(LightEffectContext context);
    }
}
