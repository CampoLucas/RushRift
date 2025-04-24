namespace Game.Entities.Components
{
    [System.Serializable]
    public class ManaComponentData : AttributeData<ManaComponent>
    {
        public override ManaComponent GetComponent()
        {
            return new ManaComponent(this);
        }
    }
}