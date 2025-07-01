using System;
using UnityEngine;

namespace Game.Entities
{
    /// <summary>
    /// Interface used to interact with the model
    /// </summary>
    public interface IModel : IDisposable
    {
        /// <summary>
        /// Initialized the model with a reference to its controller
        /// </summary>
        /// <param name="controller"></param>
        void Init(IController controller);
        /// <summary>
        /// Called every frame to update components
        /// </summary>
        /// <param name="delta"></param>
        void Update(float delta);
        /// <summary>
        /// Called after the Update to run late logic
        /// </summary>
        /// <param name="delta"></param>
        void LateUpdate(float delta);
        /// <summary>
        /// Called on physics ticks to handle physics-related logic
        /// </summary>
        /// <param name="delta"></param>
        void FixedUpdate(float delta);
        
        /// <summary>
        /// Attempts to get a component from the model
        /// </summary>
        /// <param name="component">The component to get</param>
        /// <typeparam name="TComponent">Component's type</typeparam>
        /// <returns>Returns true if it has the component</returns>
        bool TryGetComponent<TComponent>(out TComponent component) where TComponent : IEntityComponent;
        /// <summary>
        /// Attempts to add a new component to the model
        /// </summary>
        /// <param name="newComponent">The new component to add</param>
        /// <typeparam name="TComponent">Component's type</typeparam>
        /// <returns>Returns true if the component was added</returns>
        bool TryAddComponent<TComponent>(TComponent newComponent) where TComponent : IEntityComponent;
        /// <summary>
        /// Removes a component and optionally disposes it
        /// </summary>
        /// <param name="disposeComponent">If it should dispose the component when removing it</param>
        /// <typeparam name="TComponent">The component to remove</typeparam>
        /// <returns></returns>
        bool RemoveComponent<TComponent>(bool disposeComponent = true) where TComponent : IEntityComponent;
        /// <summary>
        /// Checks if the model has a component
        /// </summary>
        /// <typeparam name="TComponent">The type of component to check</typeparam>
        /// <returns>Returns true if it has the component</returns>
        bool HasComponent<TComponent>() where TComponent : IEntityComponent;
        
        // Gizmos methods
        void OnDraw(Transform transform);
        void OnDrawSelected(Transform transform);
    }
}