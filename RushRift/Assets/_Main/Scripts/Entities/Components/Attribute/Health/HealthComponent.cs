using Game.DesignPatterns.Observers;
using UnityEngine;

namespace Game.Entities.Components
{
    public class HealthComponent : Attribute<HealthComponentData, HealthComponent>
    {
        public Vector3 DamagePosition { get; private set; }
        //public ISubject OnHealthChanged { get; private set; } = new Subject();
        
        private bool _damaged;
        private float _healthLost;

        public HealthComponent(HealthComponentData data) : base(data)
        {
            LateUpdateObserver = new ActionObserver<float>(LateUpdate);
        }
        
        public void LateUpdate(float delta)
        {
            _damaged = false;
            _healthLost = 0;
        }

        public bool IsAlive() => !IsEmpty();
        public bool Damaged() => _damaged;

        public void Damage(float amount/*, DamageType dmgType*/, Vector3 position)
        {
            if (Disposed) return;
            _damaged = true;

            DamagePosition = position;
            // Calculate the final damage here.
            
            base.Decrease(amount);
        }

        public void Intakill(Vector3 position)
        {
            Damage(Value, position);
        }

        protected override void OnDecrease(float previousValue)
        {
            _damaged = true;
            _healthLost = Value - previousValue;
        }

        public override void OnDraw(Transform origin)
        {
            
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            //OnHealthChanged.Dispose();
            //OnHealthChanged = null;
        }
    }
}

