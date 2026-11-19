using Lumiscore.Core;

namespace Lumiscore.Lighting
{
    public interface ILightModuleBlender<TModule> : IStageModuleBlender<TModule>
        where TModule : class, ILightStateModule
    {
    }
}
