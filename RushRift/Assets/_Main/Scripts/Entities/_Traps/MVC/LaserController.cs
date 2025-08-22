using Game.DesignPatterns.Observers;
using Game.Entities;
using Game.Entities.Components;
using Game.LevelElements.Terminal;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.Entities
{
    public class LaserController : EntityController
    {
        private ActionObserver _onDieObserver;
        private ISubject _onDieSubject;

        protected override void Awake()
        {
            base.Awake();

            _onDieObserver = new ActionObserver(OnDieHandler);

            AddObserver(Terminal.ON_ARGUMENT, new ActionObserver(OnHandler));
            AddObserver(Terminal.OFF_ARGUMENT, new ActionObserver(OffHandler));
        }

        protected override void Start()
        {
            base.Start();

            if (GetModel().TryGetComponent<HealthComponent>(out var health))
            {
                _onDieSubject = health.OnEmptyValue;
                _onDieSubject.Attach(_onDieObserver);
            }
        }

        public sealed override Vector3 MoveDirection()
        {
            return Vector3.zero;
        }

        protected override void OnDispose()
        {
            base.OnDispose();

            _onDieSubject?.Detach(_onDieObserver);
            _onDieSubject = null;
            
            _onDieObserver?.Dispose();
            _onDieObserver = null;
        }

        private void OnDieHandler()
        {
            Destroy(gameObject);
        }

        private void OnHandler()
        {
            Debug.Log("SuperTest: Laser on");
        }

        private void OffHandler()
        {
            Debug.Log("SuperTest: Laser off");
        }
    }
}