namespace Game.Entities
{
    public abstract class EntityViewSO : SerializableSO
    {
        public abstract NullCheck<IView> GetProxy();
    }
}