namespace Game.Entities.Components
{
    public class StaminaComponent : Attribute<StaminaComponentData, StaminaComponent>
    {
        public StaminaComponent(StaminaComponentData data) : base(data)
        {
        }
    }
}