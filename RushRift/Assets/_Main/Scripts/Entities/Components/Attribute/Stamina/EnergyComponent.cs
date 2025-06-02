namespace Game.Entities.Components
{
    public class EnergyComponent : Attribute<EnergyComponentData, EnergyComponent>
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
    }
}