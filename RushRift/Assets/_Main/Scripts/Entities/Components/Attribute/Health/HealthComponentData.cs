namespace Game.Entities.Components
{
    [System.Serializable]
    public class HealthComponentData : AttributeData<HealthComponent>
    {
        public override HealthComponent GetComponent()
        {
            return new HealthComponent(this);
        }
    }
}