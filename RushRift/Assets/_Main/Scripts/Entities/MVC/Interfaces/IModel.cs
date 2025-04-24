using System;
using UnityEngine;

namespace Game.Entities
{
    public interface IModel : IDisposable
    {
        void Init(IController controller);
        void Update(float delta);
        void LateUpdate(float delta);
        void FixedUpdate(float delta);
        
        // Component methods
        bool TryGetComponent<TComponent>(out TComponent component) where TComponent : IEntityComponent;
        bool TryAddComponent<TComponent>(TComponent newComponent) where TComponent : IEntityComponent;
        bool RemoveComponent<TComponent>(bool disposeComponent = true) where TComponent : IEntityComponent;
        bool HasComponent<TComponent>() where TComponent : IEntityComponent;
        
        // Gizmos methods
        void OnDraw(Transform transform);
        void OnDrawSelected(Transform transform);
    }
}