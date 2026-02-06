using System;
using Game.DesignPatterns.Observers;
using Game.InputSystem;
using UnityEngine;

namespace Game.LevelSelector
{
    public class Interactable : MonoBehaviour
    {
        public Subject PlayerInteracted => _playerInteracted;
        
        [SerializeField] private float interactRange = 5;

        private Subject _playerInteracted = new();
        private NullCheck<Transform> _target;
        private Transform _transform;

        private void Awake()
        {
            _transform = transform;
        }
        
        private void Update()
        {
            if (!_target.TryGet(out var target) || !InRange(_transform.position, target.position) ||
                !InputManager.GetActionPerformed(InputManager.Input.Interact)) return;
            
            _playerInteracted.NotifyAll();
        }

        private bool InRange(Vector3 a, Vector3 b)
        {
            var to = a - b;
            return to.sqrMagnitude <= interactRange * interactRange;
        }
        
        public bool SetTarget(Transform target)
        {
            _target = target;

            return _target;
        }

        private void OnDestroy()
        {
            _playerInteracted.Dispose();
            _playerInteracted = null;
            _target = null;
            _transform = null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
    }
}