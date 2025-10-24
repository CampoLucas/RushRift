using Game.DesignPatterns.Observers;
using UnityEngine;

namespace Game.Entities.Components
{
    public class Attribute<TData, TDataReturn> : EntityComponent, IAttribute where TData : AttributeData<TDataReturn> where TDataReturn : IAttribute
    {
        public float Value { get; private set; }
        public float MaxValue => Data.MaxValue + _maxModifier;
        public float RegenRate => Data.RegenRate + _regenModifier;
        public float StartRegenRate => Data.RegenRate != 0 ? Data.RegenRate : .1f;
        public float StartMaxValue => Data.MaxValue;
        public ISubject<float, float, float> OnValueChanged { get; private set; } = new Subject<float, float, float>();
        public ISubject OnEmptyValue{ get; private set; } = new Subject();
        
        protected IObserver<float> LateUpdateObserver;
        protected TData Data;
        protected bool Disposed;
        
        private IObserver<float> _updateObserver;
        private float _maxModifier;
        private float _prevValue;
        
        // Regeneration variables
        private IRegenStrategy<TData, TDataReturn> _regenStrategy;
        private float _regenModifier;
        
        protected Attribute(TData data)
        {
            Data = data;
            
            _updateObserver = new ActionObserver<float>(Update);
            _regenStrategy = new RegenStrategy<TData, TDataReturn>();
            OnLoading = new NullCheck<ActionObserver<bool>>(new ActionObserver<bool>(OnLoadingHandler));
            
            InitAttribute();
        }
        
        protected virtual void Update(float delta)
        {
            if (Disposed) return;

            _regenStrategy.Tick(delta, this, Data);
        }

        public bool IsEmpty() => Value <= 0;
        public bool IsFull() => Value >= MaxValue;

        public void Decrease(float amount)
        {
            if (Disposed) return;
            if (IsEmpty()) return; // Don't decrease if it is already empty.

            _prevValue = Value;
            Value -= amount;
            
            OnDecrease(_prevValue);
            OnValueChanged.NotifyAll(Value, _prevValue, MaxValue);

            if (IsEmpty())
            {
                OnEmptyHandler();
                
            }
            else
            {
                _regenStrategy.NotifyValueChanged(_prevValue, Value, Data);
            }
            
        }

        public void Increase(float amount)
        {
            if (Disposed) return;
            var maxValue = MaxValue;

            if (Value >= maxValue)
            {
                return;
            }
            
            _prevValue = Value;

            Value += amount;
            if (Value >= maxValue)
            {
                OnFullHandler();
            }

            OnIncrease(_prevValue);
            OnValueChanged.NotifyAll(Value, _prevValue, maxValue);
            _regenStrategy.NotifyValueChanged(_prevValue, Value, Data);
        }

        public void MaxValueModifier(float amount)
        {
            _maxModifier += amount;
            OnValueChanged.NotifyAll(Value, Value, MaxValue);
        }

        public void RegenRateModifier(float amount)
        {
            _regenModifier += amount;
        }
        
        protected override void OnDispose()
        {
            Data = null;

            Disposed = true;
            
            _updateObserver.Dispose();
            _updateObserver = null;
        }

        public override bool TryGetUpdate(out IObserver<float> observer)
        {
            observer = _updateObserver;
            return _updateObserver != null;
        }

        public override bool TryGetLateUpdate(out IObserver<float> observer)
        {
            observer = LateUpdateObserver;
            return LateUpdateObserver != null;
        }

        #region Protected Methods
        
        protected virtual void OnDecrease(float previousValue) { }
        protected virtual void OnIncrease(float previousValue) { }
        protected virtual void OnEmpty() { }
        protected virtual void OnFull() { }

        #endregion

        private void InitAttribute()
        {
            var prevValue = Value;
            var startValue = Data.StartValue;
            
            Value = startValue > MaxValue ? MaxValue : startValue;
            OnValueChanged.NotifyAll(Value, prevValue, MaxValue);
            _regenStrategy.NotifyValueChanged(Value, prevValue, Data);
        }

        private void OnEmptyHandler()
        {
            Value = 0;
            
            OnEmpty();
            OnEmptyValue.NotifyAll();
        }

        private void OnFullHandler()
        {
            Value = MaxValue;
            
            OnFull();
            // It doesn't have a subject.
        }

        private void OnLoadingHandler(bool isLoading)
        {
            Reset();
        }

        protected virtual void Reset()
        {
            var startValue = Data.StartValue;
            
            Value = startValue > MaxValue ? MaxValue : startValue;
            OnValueChanged.NotifyAll(Value, startValue, MaxValue);
            _regenStrategy.NotifyValueChanged(Value, startValue, Data);
        }
    }
}
