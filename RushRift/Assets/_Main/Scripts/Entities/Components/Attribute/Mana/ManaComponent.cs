namespace Game.Entities.Components
{
    public class ManaComponent : Attribute<ManaComponentData, ManaComponent>
    {
        public ManaComponent(ManaComponentData data) : base(data)
        {
        }
    }
}