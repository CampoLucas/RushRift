using System;
using System.Collections.Generic;
using System.Linq;
using Game.DesignPatterns.Observers;
using UnityEngine;

namespace Game.Entities
{
    public class EntityModel<TData> : IModel
        where TData : EntityModelSO
    {
        protected TData Data;

        private Dictionary<Type, IEntityComponent> _componentsDict = new();
        private ISubject<float> _updateSubject = new Subject<float>();
        private ISubject<float> _lateUpdateSubject = new Subject<float>();
        private ISubject<float> _fixedUpdateSubject = new Subject<float>();

        public EntityModel(TData data)
        {
            Data = data;
        }
        
        public virtual void Init(IController controller) { }

        #region UpdateMethods

        public void Update(float delta)
        {
            _updateSubject.NotifyAll(delta);
        }

        public void LateUpdate(float delta)
        {
            _lateUpdateSubject.NotifyAll(delta);
        }

        public void FixedUpdate(float delta)
        {
            _fixedUpdateSubject.NotifyAll(delta);
        }
        
        #endregion

        #region Components

        public bool TryGetComponent<TComponent>(out TComponent component) where TComponent : IEntityComponent
        {
            component = default;
            var type = typeof(TComponent);

            if (!_componentsDict.TryGetValue(type, out var comp) || comp is not TComponent castedComponent)
                return false;

            component = castedComponent;
            return true;
        }
        
        public bool TryAddComponent<TComponent>(TComponent newComponent) where TComponent : IEntityComponent
        {
            var type = typeof(TComponent);

            if (_componentsDict.ContainsKey(type)) return false;
            
            _componentsDict[type] = newComponent;

            if (newComponent.TryGetUpdate(out var update))
            {
                _updateSubject.Attach(update);
            }

            if (newComponent.TryGetLateUpdate(out var lateUpdate))
            {
                _lateUpdateSubject.Attach(lateUpdate);
            }

            if (newComponent.TryGetFixedUpdate(out var fixedUpdate))
            {
                _fixedUpdateSubject.Attach(fixedUpdate);
            }

            return true;
        }
        
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
        
        public bool HasComponent<TComponent>() where TComponent : IEntityComponent
        {
            return _componentsDict.ContainsKey(typeof(TComponent));
        }

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

            Data = null;
        }
    }
}