using Game.DesignPatterns.Observers;
using UnityEngine;

namespace Game.Entities.Components
{
    public class Attribute<TData, TDataReturn> : IAttribute where TData : AttributeData<TDataReturn> where TDataReturn : IAttribute
    {
        public float Value { get; private set; }
        public float MaxValue => _data.MaxValue;
        public ISubject<(float, float, float)> OnValueChanged { get; private set; } = new Subject<(float, float, float)>();
        public ISubject OnValueDepleted{ get; private set; } = new Subject();

        
        protected IObserver<float> LateUpdateObserver;
        protected bool Disposed;
        
        private IObserver<float> _updateObserver;
        private TData _data;
        
        // ToDo: observers for when value updated, value depleted, value maxed, maxModifierUpdated, regenModifierUpdated
        // ToDo: when adding a max value modifier and the attribute is full, the value will stay full.
        // ToDo: when removing a max value modifier adjust the value so it is not over the max value.
        // ToDo: have a changed max value and check if the current max value is different and if it is, adjust the value if nescesary
        
        public Attribute(TData data)
        {
            _data = data;
            
            _updateObserver = new ActionObserver<float>(Update);
            
            InitAttribute();
        }
        
        public void Update(float delta)
        {
            if (Disposed) return;
            if (Value < _data.MaxValue)
            {
                Increase(delta * _data.RegenRate);
            }
        }

        public bool IsEmpty() => Value <= 0;
        public bool IsFull() => Value >= _data.MaxValue;

        public void Decrease(float amount)
        {
            if (Disposed) return;
            if (IsEmpty()) return; // Don't decrease if it is already empty.

            var prevValue = Value;
            Value -= amount;
            
            OnDecrease(prevValue);
            OnValueChanged.NotifyAll((Value, prevValue, _data.MaxValue));

            if (IsEmpty())
            {
                OnValueDepleted.NotifyAll();
            }
        }

        public void Increase(float amount)
        {
            if (Disposed) return;
            var maxValue = _data.MaxValue;
            
            if (Value >= maxValue) return;
            var prevValue = Value;

            Value += amount;
            if (Value >= maxValue)
            {
                Value = maxValue;
            }

            OnIncrease(prevValue);
            OnValueChanged.NotifyAll((Value, prevValue, maxValue));
        }
        
        public void Dispose()
        {
            OnDispose();
            _data = null;

            Disposed = true;
            
            _updateObserver.Dispose();
            _updateObserver = null;
        }

        public bool TryGetUpdate(out IObserver<float> observer)
        {
            observer = _updateObserver;
            return _updateObserver != null;
        }

        public bool TryGetLateUpdate(out IObserver<float> observer)
        {
            observer = LateUpdateObserver;
            return LateUpdateObserver != null;
        }

        public bool TryGetFixedUpdate(out IObserver<float> observer)
        {
            observer = default;
            return false;
        }

        public virtual void OnDraw(Transform origin) { }
        public virtual void OnDrawSelected(Transform origin) { }

        #region Protected Methods
        
        protected virtual void OnDecrease(float previousValue) { }
        protected virtual void OnIncrease(float previousValue) { }
        protected virtual void OnDispose() { }

        #endregion

        private void InitAttribute()
        {
            var prevValue = Value;
            var startValue = _data.StartValue;
            
            Value = startValue > _data.MaxValue ? _data.MaxValue : startValue;
            OnValueChanged.NotifyAll((Value, prevValue, _data.MaxValue));
        }
    }
}
