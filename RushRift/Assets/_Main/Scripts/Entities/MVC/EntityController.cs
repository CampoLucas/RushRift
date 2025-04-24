using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.Entities
{
    public abstract class EntityController : MonoBehaviour, IController
    {
        public Transform Transform { get; private set; }
        public Transform EyesTransform { get; protected set; }

        [Header("Data")]
        [SerializeField] protected EntityModelSO model;
        [SerializeField] protected EntityViewSO view;

        [Header("References")]
        [SerializeField] private Animator animator;

        private IModel _model;
        private IView _view;

        private void Awake()
        {
            Transform = transform;

            // Create the model
            if (model.GetProxy().TryGetValue(out _model))
            {
                _model.Init(this);
            }

            if (view.GetProxy().TryGetValue(out _view))
            {
                _view.Init(animator);
            }
        }

        protected virtual void Update()
        {
            var delta = Time.deltaTime;
            
            _model.Update(delta);
        }

        protected void LateUpdate()
        {
            var delta = Time.deltaTime;
            _model.LateUpdate(delta);
        }

        protected void FixedUpdate()
        {
            var delta = Time.fixedDeltaTime;
            _model.FixedUpdate(delta);
        }

        public IModel GetModel() => _model;
        public IView GetView() => _view;
        public abstract Vector3 MoveDirection();
        protected virtual void InitStateMachine() { }

        public void OnDrawGizmos()
        {
            if (_model != null) _model.OnDraw(transform);
        }

        public void OnDrawGizmosSelected()
        {
            if (_model != null) _model.OnDrawSelected(transform);
        }

        public void Dispose()
        {
            if (_model != null) _model.Dispose();
            _model = null;
            
            if (_view != null) _view.Dispose();
            _view = null;
        }

        public void OnDestroy()
        {
            Dispose();
        }
    }
}
