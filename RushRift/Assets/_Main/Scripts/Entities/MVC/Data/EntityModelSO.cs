namespace Game.Entities
{
    public abstract class EntityModelSO : SerializableSO
    {
        public abstract NullCheck<IModel> GetProxy();
    }
}