using Lumiscore.State;

namespace Lumiscore.Drivers
{
    public interface IFixtureDriver<TState>
        where TState : IFixtureState
    {
        TState CaptureState();
        void ApplyState(TState state);
    }
}
