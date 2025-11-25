using Game.DesignPatterns.Observers;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Entities.Components
{
    public sealed class EnergyComponent : Attribute<EnergyComponentData, EnergyComponent>
    {
        public bool IsRefilling { get; private set; }
        public float RefillProgress { get; private set; }
        public ISubject<float> OnRefill = new Subject<float>();
        private float _extraTimer;
        
        public EnergyComponent(EnergyComponentData data) : base(data)
        {
        }

        protected override void Update(float delta)
        {
            base.Update(delta);
            if (_extraTimer <= 0)
            {
                if (Value <= 0)
                {
                    IsRefilling = false;
                    Increase(Data.ExtraAmount);
                    SetRefillProgress(1);
                }
                
                return;
            }

            SetRefillProgress(Mathf.Clamp01(_extraTimer / Data.ExtraTime));
            _extraTimer -= delta;
        }

        protected override void OnDecrease(float previousValue)
        {
            base.OnDecrease(previousValue);
            if (Value <= 0)
            {
                _extraTimer = Data.ExtraTime;
                IsRefilling = true;
                SetRefillProgress(0);
            }
        }

        protected override void OnIncrease(float previousValue)
        {
            IsRefilling = false;
            base.OnIncrease(previousValue);
        }

        protected override void Reset()
        {
            _extraTimer = 0;
            base.Reset();
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            OnRefill.Dispose();
        }

        private void SetRefillProgress(float amount)
        {
            RefillProgress = amount;
            OnRefill.NotifyAll(amount);
        }
    }
}