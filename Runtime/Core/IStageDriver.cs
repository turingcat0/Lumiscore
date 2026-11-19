namespace Lumiscore.Core
{
    public interface IStageDriver<TState>
        where TState : IStageState
    {
        TState CaptureState();
        void ApplyState(TState state);
    }
}
