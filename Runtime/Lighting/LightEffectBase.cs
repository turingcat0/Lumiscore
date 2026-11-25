using System;
using Lumiscore.Core;

namespace Lumiscore.Lighting
{
    /// <summary>
    /// Strongly-typed base class for lighting effects targeting <typeparamref name="TModule"/>.
    /// </summary>
    [Serializable]
    public abstract class LightEffectBase<TModule> : StageEffectBase
        where TModule : class, ILightStateModule
    {
        /// <summary>
        /// Stateless pure evaluation for all direct children of the track's target node.
        /// Write output values directly into the pre-allocated <paramref name="results"/> array without allocating.
        /// </summary>
        public abstract void Evaluate(LightEffectContext context, TModule[] results);
    }
}
