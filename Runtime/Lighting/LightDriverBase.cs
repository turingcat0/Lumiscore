using System;
using Lumiscore.Core;
using UnityEngine;

namespace Lumiscore.Lighting
{
    public abstract class LightDriverBase<TState> : MonoBehaviour, ILightDriver<TState>
        where TState : class, ILightState
    {
        public Type StateType => typeof(TState);

        public abstract TState CaptureState();
        public abstract void ApplyState(TState state);

        IStageState IStageDriver.CaptureState() => CaptureState();

        void IStageDriver.ApplyState(IStageState state)
        {
            if (state is TState typedState)
            {
                ApplyState(typedState);
            }
        }
    }
}
