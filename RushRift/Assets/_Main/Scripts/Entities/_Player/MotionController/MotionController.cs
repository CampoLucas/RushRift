using System;
using System.Collections.Generic;
using System.Linq;
using Game.DesignPatterns.Observers;
using Game.UI;
using UnityEngine;

namespace Game.Entities.Components.MotionController
{
    public class MotionController : IEntityComponent
    {
        private Rigidbody _rb;
        private MotionContext _context;
        private List<BaseMotionHandler> _handlers;

        private DesignPatterns.Observers.IObserver<float> _updateObserver;
        private DesignPatterns.Observers.IObserver<float> _lateUpdateObserver;
        private DesignPatterns.Observers.IObserver<float> _fixedUpdateObserver;
        private IObserver _onPaused;
        private IObserver _onUnPaused;
        
        private Vector3 _pauseVelocity;
        private RigidbodyConstraints _pauseConstrains;

        public MotionController(Rigidbody rigidBody, CapsuleCollider collider, Transform orientation, Transform look, MotionConfig[] handlerConfigs)
        {
            _rb = rigidBody;
            // _orientationTransform = orientation;
            // _lookTransform = look;

            _context = new MotionContext(rigidBody, collider, rigidBody.transform, look, orientation);
            _handlers = BuildHandlers(handlerConfigs);

            _onPaused = new ActionObserver(PauseHandler);
            _onUnPaused = new ActionObserver(UnPauseHandler);
            
            UIManager.OnPaused.Attach(_onPaused);
            UIManager.OnUnPaused.Attach(_onUnPaused);
        }

        private void Update(float delta)
        {
            foreach (var t in _handlers)
            {
                t.OnUpdate(_context, delta);
            }
        }

        private void LateUpdate(float delta)
        {
            foreach (var t in _handlers)
            {
                t.OnLateUpdate(_context, delta);
            }

            // _context.Dash = false;
            // _context.Jump = false;
        }

        private void FixedUpdate(float delta)
        {
            foreach (var t in _handlers)
            {
                t.OnFixedUpdate(_context, delta);
            }
        }

        private void PauseHandler()
        {
            Debug.Log("Pause");
            _pauseVelocity = _rb.velocity;
            _pauseConstrains = _rb.constraints;
            
            _rb.velocity = Vector3.zero;
            _rb.constraints = RigidbodyConstraints.FreezeAll;
            
            _rb.isKinematic = true;
        }

        private void UnPauseHandler()
        {
            _rb.isKinematic = false;
            
            _rb.constraints = _pauseConstrains;
            _rb.velocity = _pauseVelocity;
        }

        private List<BaseMotionHandler> BuildHandlers(MotionConfig[] configs)
        {
            var handlers = new List<(float order, BaseMotionHandler handler)>();

            foreach (var config in configs)
            {
                if (config == null || !config.Enabled)
                    continue;

                var handler = config.GetHandler();
                if (handler != null)
                    handlers.Add((config.Order, handler));
            }

            handlers.Sort((a, b) => a.order.CompareTo(b.order));

            return handlers.Select(pair => pair.handler).ToList();
        }
        
        public bool TryGetUpdate(out DesignPatterns.Observers.IObserver<float> observer)
        {
            _updateObserver ??= new ActionObserver<float>(Update);
            observer = _updateObserver;
            return true;
        }

        public bool TryGetLateUpdate(out DesignPatterns.Observers.IObserver<float> observer)
        {
            _lateUpdateObserver ??= new ActionObserver<float>(LateUpdate);
            observer = _lateUpdateObserver;
            return true;
        }

        public bool TryGetFixedUpdate(out DesignPatterns.Observers.IObserver<float> observer)
        {
            _fixedUpdateObserver ??= new ActionObserver<float>(FixedUpdate);
            observer = _fixedUpdateObserver;
            return true;
        }

        public void OnDraw(Transform origin)
        {
            foreach (var t in _handlers)
            {
                t.OnDraw(origin);
            }
        }

        public void OnDrawSelected(Transform origin)
        {
            foreach (var t in _handlers)
            {
                t.OnDraw(origin);
            }
        }
        
        public void Dispose()
        {
            UIManager.OnPaused.Attach(_onPaused);
            UIManager.OnUnPaused.Attach(_onUnPaused);
            
            _onPaused?.Dispose();
            _onUnPaused?.Dispose();

            _onPaused = null;
            _onUnPaused = null;
            
            foreach (var t in _handlers)
            {
                t?.Dispose();
            }
            
            _handlers?.Clear();
            _context?.Dispose();

            _handlers = null;
            _context = null;
            // _orientationTransform = null;
            // _lookTransform = null;
            _rb = null;
            
            _updateObserver?.Dispose();
            _updateObserver = null;
            
            _fixedUpdateObserver?.Dispose();
            _fixedUpdateObserver = null;
            
            _lateUpdateObserver?.Dispose();
            _lateUpdateObserver = null;
        }

        public void Dash()
        {
            _context.Dash = true;
        }

        public void Jump()
        {
            _context.Jump = true;
        }
    }
}