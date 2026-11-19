namespace Lumiscore.Core
{
    public interface IStageModuleBlender<TModule>
        where TModule : class, IStageStateModule
    {
        TModule Blend(
            TModule from,
            TModule to,
            float weight,
            StageBlendMode mode = StageBlendMode.Override);
    }
}
