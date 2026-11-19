using Lumiscore.Core;

namespace Lumiscore.Lighting
{
    public interface ILightDriver<TState> : IStageDriver<TState>
        where TState : ILightState
    {
    }
}
