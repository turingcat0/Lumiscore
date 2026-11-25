using System;
using Lumiscore.Core;

namespace Lumiscore.Lighting
{
    [Serializable]
    public abstract class LightEffectBase<TModule> : StageEffectBase
        where TModule : class, ILightStateModule
    {
        public abstract void Evaluate(LightEffectContext context, TModule[] results);
    }
}
