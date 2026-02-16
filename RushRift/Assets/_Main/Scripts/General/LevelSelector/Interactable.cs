using System;
using Game.DesignPatterns.Observers;
using Game.InputSystem;
using MyTools.Global;
using UnityEngine;

namespace Game.LevelSelector
{
    public class Interactable : MonoBehaviour
    {
        public Subject PlayerInteracted => _playerInteracted;
        public Subject<bool> PlayerInRange => _playerInRange;
        public bool IsInRange => _onRangeState;
        
        [SerializeField] private float interactRange = 5;

        private Subject _playerInteracted = new();
        private Subject<bool> _playerInRange = new();
        private NullCheck<Transform> _target;
        private Transform _transform;
        private bool _onRangeState;
        private bool _onRangePrevState;

        private void Awake()
        {
            _transform = transform;
        }
        
        private void Update()
        {
            if (Detect())
            {
                _playerInteracted.NotifyAll();
            }
            

            _onRangePrevState = _onRangeState;
        }

        private bool Detect()
        {
            if (!_target.TryGet(out var target) || !InRange(_transform.position, target.position))
            {
                if (_onRangePrevState)
                {
                    _playerInRange.NotifyAll(false);
                    _onRangeState = false;
                }
                return false;
            }

            if (!_onRangePrevState)
            {
                _playerInRange.NotifyAll(true);
                _onRangeState = true;
            }
            
            return InputManager.GetActionPerformed(InputManager.Input.Interact);
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
            _playerInRange.Dispose();
            _playerInRange = null;
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