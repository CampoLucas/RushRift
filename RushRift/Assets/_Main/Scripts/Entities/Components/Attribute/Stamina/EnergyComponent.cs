namespace Game.Entities.Components
{
    public sealed class EnergyComponent : Attribute<EnergyComponentData, EnergyComponent>
    {
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
                    Increase(Data.ExtraAmount);
                }
                
                return;
            }

            _extraTimer -= delta;
        }

        protected override void OnDecrease(float previousValue)
        {
            base.OnDecrease(previousValue);
            if (Value <= 0) _extraTimer = Data.ExtraTime;
        }

        protected override void Reset()
        {
            _extraTimer = 0;
            base.Reset();
        }
    }
}