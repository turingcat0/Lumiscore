using System;
using Lumiscore.Core;

namespace Lumiscore.Lighting
{
    /// <summary>
    /// Strongly-typed base class for lighting fixture drivers targeting <typeparamref name="TState"/>.
    /// Concrete subclasses must be non-generic and marked <see cref="SerializableAttribute"/>.
    /// </summary>
    [Serializable]
    public abstract class LightDriverBase<TState> : StageDriverBase
        where TState : class, ILightState
    {
        public override Type StateType => typeof(TState);

        public abstract void ApplyState(TState state);

        protected override void ApplyStateCore(IStageState state)
        {
            if (state is TState typedState)
            {
                ApplyState(typedState);
            }
        }
    }
}
