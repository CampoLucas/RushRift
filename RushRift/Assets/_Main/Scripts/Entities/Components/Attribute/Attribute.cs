using Game.DesignPatterns.Observers;
using UnityEngine;

namespace Game.Entities.Components
{
    public class Attribute<TData, TDataReturn> : IAttribute where TData : AttributeData<TDataReturn> where TDataReturn : IAttribute
    {
        public float Value { get; private set; }
        public float MaxValue => _data.MaxValue + _maxModifier;
        public float RegenRate => _data.RegenRate + _regenModifier;
        public float StartRegenRate => _data.RegenRate != 0 ? _data.RegenRate : .1f;
        public float StartMaxValue => _data.MaxValue;
        public ISubject<float, float, float> OnValueChanged { get; private set; } = new Subject<float, float, float>();
        public ISubject OnValueDepleted{ get; private set; } = new Subject();
        
        protected IObserver<float> LateUpdateObserver;
        protected bool Disposed;
        
        private IObserver<float> _updateObserver;
        private TData _data;
        private float _maxModifier;
        private float _prevValue;
        
        // Regeneration variables
        private IRegenStrategy<TData, TDataReturn> _regenStrategy;
        // private bool _regenerating;
        // private bool _startRegenDelay;
        // private float _regenDelayTimer;
        private float _regenModifier;
        
        public Attribute(TData data)
        {
            _data = data;
            
            _updateObserver = new ActionObserver<float>(Update);
            _regenStrategy = new RegenStrategy<TData, TDataReturn>();
            
            InitAttribute();
        }
        
        public void Update(float delta)
        {
            if (Disposed/* || !_data.HasRegen*/) return;

            _regenStrategy.Tick(delta, this, _data);
            
            /*
            if (!_regenerating && !_startRegenDelay && _prevValue != Value)
            {
                _startRegenDelay = true;
                _regenDelayTimer = _data.RegenDelay;
            }

            if (_startRegenDelay)
            {
                _regenDelayTimer -= delta;

                if (_regenDelayTimer <= 0)
                {
                    _startRegenDelay = false;
                    _regenerating = true;
                }
            }

            if (_regenerating)
            {
                if (_data.RegenRate > 0)
                {
                    if (Value < MaxValue)
                    {
                        Increase(delta * RegenRate);
                    }
                    else
                    {
                        _regenerating = false;
                    }
                }
                else
                {
                    if (Value > 0)
                    {
                        Decrease(delta * Mathf.Abs(RegenRate));
                    }
                    else
                    {
                        _regenerating = false;
                    }
                }
            }
            */
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
                Value = 0;
                OnValueDepleted.NotifyAll();
            }
            else
            {
                _regenStrategy.NotifyValueChanged(_prevValue, Value, _data);
            }
        }

        public void Increase(float amount)
        {
            if (Disposed) return;
            var maxValue = MaxValue;
            
            if (Value >= maxValue) return;
            _prevValue = Value;

            Value += amount;
            if (Value >= maxValue)
            {
                Value = maxValue;
            }

            OnIncrease(_prevValue);
            OnValueChanged.NotifyAll(Value, _prevValue, maxValue);
            _regenStrategy.NotifyValueChanged(_prevValue, Value, _data);
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
            
            Value = startValue > MaxValue ? MaxValue : startValue;
            OnValueChanged.NotifyAll(Value, prevValue, MaxValue);
            _regenStrategy.NotifyValueChanged(Value, prevValue, _data);
        }
    }
}
