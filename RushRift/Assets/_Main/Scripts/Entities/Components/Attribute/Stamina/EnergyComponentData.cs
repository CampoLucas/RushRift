namespace Game.Entities.Components
{
    [System.Serializable]
    public class EnergyComponentData : AttributeData<EnergyComponent>
    {
        public override EnergyComponent GetComponent()
        {
            return new EnergyComponent(this);
        }
    }
}