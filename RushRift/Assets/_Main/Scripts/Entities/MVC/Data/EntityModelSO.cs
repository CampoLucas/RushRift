namespace Game.Entities
{
    /// <summary>
    /// Base ScriptableObject for creating runtime Model proxies
    /// </summary>
    public abstract class EntityModelSO : SerializableSO
    {
        /// <summary>
        /// Creates a proxy instance to be used during runtime 
        /// </summary>
        /// <returns>A new model proxy</returns>
        public abstract NullCheck<IModel> GetProxy();
    }
}