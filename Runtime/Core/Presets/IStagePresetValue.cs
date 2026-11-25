using System;
using UnityEngine;

namespace Lumiscore.Core
{
    public interface IStagePresetValue
    {
        Type ValueType { get; }
        object GetBoxedValue();
        void SetBoxedValue(object value);
        IStagePresetValue Clone();
    }

    [Serializable]
    public abstract class StagePresetValueBase<T> : IStagePresetValue
    {
        [SerializeField] private T _value;

        public T Value
        {
            get => _value;
            set => _value = value;
        }

        public Type ValueType => typeof(T);

        protected StagePresetValueBase()
        {
        }

        protected StagePresetValueBase(T value)
        {
            _value = value;
        }

        public object GetBoxedValue() => CloneValue(_value);

        public void SetBoxedValue(object value)
        {
            _value = value is T typed ? CloneValue(typed) : default;
        }

        public abstract IStagePresetValue Clone();

        protected virtual T CloneValue(T source) => source;
    }
}
