using Lumiscore.Core;

namespace Lumiscore.Lighting
{
    public interface ILightState : IStageState
    {
        TModule GetModule<TModule>() where TModule : class, ILightStateModule;
        void SetModule<TModule>(TModule module) where TModule : class, ILightStateModule;
        ILightState Clone();
    }

    public interface IHasLightModule<TModule> : ILightState
        where TModule : class, ILightStateModule
    {
    }
}
