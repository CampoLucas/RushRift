namespace Game.Entities.Components
{
    [System.Serializable]
    public class StaminaComponentData : AttributeData<StaminaComponent>
    {
        public override StaminaComponent GetComponent()
        {
            return new StaminaComponent(this);
        }
    }
}