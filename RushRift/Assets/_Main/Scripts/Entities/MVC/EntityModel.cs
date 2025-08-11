using System;
using System.Collections.Generic;
using System.Linq;
using Game.DesignPatterns.Observers;
using UnityEngine;

namespace Game.Entities
{
    /// <summary>
    /// Generic runtime model created from the ScriptableObject
    /// </summary>
    /// <typeparam name="TData">The type of ModelSO</typeparam>
    public class EntityModel<TData> : IModel
        where TData : EntityModelSO
    {
        private TData _data; // Reference to the data (SO)

        private Dictionary<Type, IEntityComponent> _componentsDict = new();
        private ISubject<float> _updateSubject = new Subject<float>();
        private ISubject<float> _lateUpdateSubject = new Subject<float>();
        private ISubject<float> _fixedUpdateSubject = new Subject<float>();

        public EntityModel(TData data)
        {
            _data = data;
        }

        /// <summary>
        /// Initializes the model with a controller reference
        /// </summary>
        /// <param name="controller">The assigned controller for the model</param>
        public virtual void Init(IController controller)
        {
            _data.Init(controller, this);
        }

        #region UpdateMethods

        /// <summary>
        /// Notifies components of the Update call
        /// </summary>
        /// <param name="delta">The time between frames</param>
        public void Update(float delta)
        {
            _updateSubject.NotifyAll(delta);
        }

        /// <summary>
        /// Notifies components of the Late Update call
        /// </summary>
        /// <param name="delta">The time between frames</param>
        public void LateUpdate(float delta)
        {
            _lateUpdateSubject.NotifyAll(delta);
        }

        /// <summary>
        /// Notifies components of the Fixed Update call
        /// </summary>
        /// <param name="delta">The time between fixed frames</param>
        public void FixedUpdate(float delta)
        {
            _fixedUpdateSubject.NotifyAll(delta);
        }
        
        #endregion

        #region Components

        /// <summary>
        /// Attempts to get a component from the model
        /// </summary>
        /// <param name="component">The component to get</param>
        /// <typeparam name="TComponent">Component's type</typeparam>
        /// <returns>Returns true if it has the component</returns>
        public bool TryGetComponent<TComponent>(out TComponent component) where TComponent : IEntityComponent
        {
            component = default;
            var type = typeof(TComponent);

            if (!_componentsDict.TryGetValue(type, out var comp) || comp is not TComponent castedComponent)
                return false;

            component = castedComponent;
            return true;
        }
        
        /// <summary>
        /// Attempts to add a new component to the model
        /// </summary>
        /// <param name="newComponent">The new component to add</param>
        /// <typeparam name="TComponent">Component's type</typeparam>
        /// <returns>Returns true if the component was added</returns>
        public bool TryAddComponent<TComponent>(TComponent newComponent) where TComponent : IEntityComponent
        {
            if (newComponent == null)
            {
                return false;
            }
            
            var type = typeof(TComponent);

            if (!_componentsDict.TryAdd(type, newComponent))
            {
                newComponent.Dispose();
                return false;
            }
            
            if (newComponent.TryGetUpdate(out var update)) // If it uses an update, it subscribes it's observer.
            {
                _updateSubject.Attach(update);
            }

            if (newComponent.TryGetLateUpdate(out var lateUpdate)) // If it uses a late update, it subscribes it's observer.
            {
                _lateUpdateSubject.Attach(lateUpdate);
            }

            if (newComponent.TryGetFixedUpdate(out var fixedUpdate)) // If it uses a fixed update, it subscribes it's observer.
            {
                _fixedUpdateSubject.Attach(fixedUpdate);
            }

            return true;
        }
        
        /// <summary>
        /// Removes a component and optionally disposes it
        /// </summary>
        /// <param name="disposeComponent">If it should dispose the component when removing it</param>
        /// <typeparam name="TComponent">The component to remove</typeparam>
        /// <returns></returns>
        public bool RemoveComponent<TComponent>(bool disposeComponent = true) where TComponent : IEntityComponent
        {
            var type = typeof(TComponent);
            if (!_componentsDict.Remove(type, out var component)) return false;
            
            if (component.TryGetUpdate(out var update))
            {
                _updateSubject.Detach(update);
            }

            if (component.TryGetLateUpdate(out var lateUpdate))
            {
                _lateUpdateSubject.Detach(lateUpdate);
            }

            if (component.TryGetFixedUpdate(out var fixedUpdate))
            {
                _fixedUpdateSubject.Detach(fixedUpdate);
            }
            
            if (disposeComponent) component.Dispose();

            return true;
        }
        
        /// <summary>
        /// Checks if the model has a component
        /// </summary>
        /// <typeparam name="TComponent">The type of component to check</typeparam>
        /// <returns>Returns true if it has the component</returns>
        public bool HasComponent<TComponent>() where TComponent : IEntityComponent
        {
            return _componentsDict.ContainsKey(typeof(TComponent));
        }

        /// <summary>
        /// Disposes all components and clears all observers
        /// </summary>
        public void RemoveAllComponents()
        {
            _updateSubject.DetachAll();
            _lateUpdateSubject.DetachAll();
            _fixedUpdateSubject.DetachAll();

            var keys = _componentsDict.Keys;

            foreach (var key in keys)
            {
                _componentsDict[key].Dispose();
            }
            
            _componentsDict.Clear();
        }

        #endregion

        #region Draw

        public void OnDraw(Transform transform)
        {
#if UNITY_EDITOR
            var keys = _componentsDict.Keys.ToList();
            for (var i = 0; i < keys.Count; i++)
            {
                var comp = _componentsDict[keys[i]];
                comp?.OnDraw(transform);
            }
#endif
        }

        public void OnDrawSelected(Transform transform)
        {
#if UNITY_EDITOR
            var keys = _componentsDict.Keys.ToList();
            for (var i = 0; i < keys.Count; i++)
            {
                var comp = _componentsDict[keys[i]];
                comp?.OnDrawSelected(transform);
            }
#endif
        }

        #endregion

        /// <summary>
        /// Disposes model and it's references
        /// </summary>
        public void Dispose()
        {
            RemoveAllComponents();
            
            _updateSubject.Dispose();
            _lateUpdateSubject.Dispose();
            _fixedUpdateSubject.Dispose();
            
            _updateSubject = null;
            _lateUpdateSubject = null;
            _fixedUpdateSubject = null;

            _componentsDict = null;

            _data = null;
        }
    }
}