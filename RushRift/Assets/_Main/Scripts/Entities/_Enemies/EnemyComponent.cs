using Game.DesignPatterns.Observers;
using Game.Entities.Components;
using UnityEngine;

namespace Game.Entities.Enemies.Components
{
    public sealed class EnemyComponent : EntityComponent
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
        
        
        protected override void OnDispose()
        {
            _target.Dispose();
        }
    }
}