using Game.DesignPatterns.Observers;

namespace Game.Entities.Components
{
    /// <summary>
    /// A interface for classes that handle health, mana and stamina.
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    public interface IAttribute : IEntityComponent
    {
        /// <summary>
        /// The current value of the attribute.
        /// </summary>
        float Value { get; }
        float MaxValue { get; }

        /// <summary>
        /// A subject that notifies observers when the value is updated.
        /// It takes a tuple with the current, previous and max value.
        /// </summary>
        ISubject<float, float, float> OnValueChanged { get; }
        
        /// <summary>
        /// Indicates if the value of the attribute is less or equal than zero.
        /// </summary>
        /// <returns></returns>
        bool IsEmpty();
        /// <summary>
        /// Indicates if the value of the attribute is equal to the max value.
        /// </summary>
        /// <returns></returns>
        bool IsFull();
        /// <summary>
        /// Reduces the attribute's value.
        /// </summary>
        /// <param name="amount">The amount to reduce.</param>
        void Decrease(float amount);
        /// <summary>
        /// Increases the attribute's value
        /// </summary>
        /// <param name="amount">The amount to increase.</param>
        void Increase(float amount);
    }
}