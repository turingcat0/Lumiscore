using System;

namespace Lumiscore.Core
{
    public interface IStageDriver
    {
        Type StateType { get; }
        IStageState CaptureState();
        void ApplyState(IStageState state);
    }

    public interface IStageDriver<TState> : IStageDriver
        where TState : IStageState
    {
        new TState CaptureState();
        void ApplyState(TState state);
    }
}
