using Game.DesignPatterns.Observers;
using Game.Entities.Components;
using UnityEngine;

namespace Game.Entities.Enemies.Components
{
    public class EnemyComponent : IEntityComponent
    {
        private NullCheck<Transform> _target;
        private bool _isFollowing;

        public bool TryGetTarget(out Transform target)
        {
            if (_target)
            {
                target = _target.Get();
                return true;
            }

            target = null;
            return false;
        }

        public bool HasTarget() => _target;
        public void SetTarget(Transform newTarget) => _target.Set(newTarget);
        public void SetFollowing(bool value) => _isFollowing = value;
        public bool IsFollowing() => _isFollowing;

        public bool TryGetUpdate(out IObserver<float> observer)
        {
            observer = default;
            return false;
        }

        public bool TryGetLateUpdate(out IObserver<float> observer)
        {
            observer = default;
            return false;
        }

        public bool TryGetFixedUpdate(out IObserver<float> observer)
        {
            observer = default;
            return false;
        }

        public void OnDraw(Transform origin)
        {
            
        }

        public void OnDrawSelected(Transform origin)
        {
            
        }
        
        public void Dispose()
        {
            
        }
    }
}