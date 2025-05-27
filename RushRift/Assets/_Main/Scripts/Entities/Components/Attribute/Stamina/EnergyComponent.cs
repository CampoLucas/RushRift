namespace Game.Entities.Components
{
    public class EnergyComponent : Attribute<EnergyComponentData, EnergyComponent>
    {
        public EnergyComponent(EnergyComponentData data) : base(data)
        {
        }
    }
}