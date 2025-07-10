namespace Game.Entities
{
    /// <summary>
    /// ScriptableObject used to configure and create a view proxy at runtime
    /// </summary>
    public abstract class EntityViewSO : SerializableSO
    {
        /// <summary>
        /// Creates a proxy instance to be used during runtime 
        /// </summary>
        /// <returns>A new view proxy</returns>
        public abstract NullCheck<IView> GetProxy();
    }
}