namespace Lumiscore.Core
{
    /// <summary>
    /// Stateless blending and easing policy for a single <see cref="IStageStateModule"/> type.
    /// Implementations should write into the pre-allocated <c>result</c> instance without allocating.
    /// </summary>
    public interface IStageModuleMixer<TModule>
        where TModule : class, IStageStateModule
    {
        /// <summary>Composes <paramref name="to"/> onto <paramref name="from"/> using <paramref name="mode"/> and <paramref name="weight"/>.</summary>
        void Blend(
            TModule from,
            TModule to,
            float weight,
            StageBlendMode mode,
            TModule result);

        /// <summary>Transitions from <paramref name="from"/> to <paramref name="to"/> as <paramref name="normalizedTime"/> moves from 0 to 1.</summary>
        void EaseIn(
            TModule from,
            TModule to,
            float normalizedTime,
            TModule result);

        /// <summary>Transitions out of <paramref name="to"/> back to <paramref name="from"/> as <paramref name="normalizedTime"/> moves from 0 to 1.</summary>
        void EaseOut(
            TModule from,
            TModule to,
            float normalizedTime,
            TModule result);
    }
}
