using UnityEngine;

namespace Game.Entities.Components
{
    public struct RegenStrategy<TData, TDataReturn> : IRegenStrategy<TData, TDataReturn>
        where TData : AttributeData<TDataReturn> where TDataReturn : IAttribute
    {
        private float _regenDelayTimer;
        private bool _regenerating;
        private bool _waitingForDelay;
        
        public void Tick(float delta, Attribute<TData, TDataReturn> attribute, TData data)
        {
            if (!data.HasRegen || attribute.IsEmpty()) return;

            if (_waitingForDelay)
            {
                _regenDelayTimer -= delta;
                if (_regenDelayTimer <= 0)
                {
                    _waitingForDelay = false;
                    _regenerating = true;
                }
            }

            if (_regenerating)
            {
                var regenRate = attribute.RegenRate;
                if (regenRate > 0)
                {
                    if (!attribute.IsFull()) attribute.Increase(delta * regenRate);
                    else _regenerating = false;
                }
                else
                {
                    if (!attribute.IsEmpty()) attribute.Decrease(delta * Mathf.Abs(regenRate));
                    else _regenerating = false;
                }
            }
        }

        public void NotifyValueChanged(float oldValue, float newValue, TData data)
        {
            if (!data.HasRegen || oldValue == newValue) return;

            _waitingForDelay = true;
            _regenDelayTimer = data.RegenDelay;
            _regenerating = false;
        }
        
        public void Dispose()
        {
            
        }
    }
}