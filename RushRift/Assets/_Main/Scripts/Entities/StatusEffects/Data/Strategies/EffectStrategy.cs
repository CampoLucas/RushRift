namespace Game.Entities
{
    public abstract class EffectStrategy : SerializableSO, IEffectStrategy
    {
        public abstract void StartEffect(IController controller);
        public abstract void StopEffect(IController controller);
        
        public void Dispose()
        {
            
        }
    }
}