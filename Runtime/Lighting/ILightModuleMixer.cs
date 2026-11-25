using Lumiscore.Core;

namespace Lumiscore.Lighting
{
    public interface ILightModuleMixer<TModule> : IStageModuleMixer<TModule>
        where TModule : class, ILightStateModule
    {
    }
}
