namespace Lumiscore.Core
{
    public interface IStageModuleMixer<TModule>
        where TModule : class, IStageStateModule
    {
        void Blend(
            TModule from,
            TModule to,
            float weight,
            StageBlendMode mode,
            TModule result);

        void EaseIn(
            TModule from,
            TModule to,
            float normalizedTime,
            TModule result);

        void EaseOut(
            TModule from,
            TModule to,
            float normalizedTime,
            TModule result);
    }
}
