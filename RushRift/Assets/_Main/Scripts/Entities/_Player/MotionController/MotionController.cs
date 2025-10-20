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
        private NullCheck<Rigidbody> _rb;
        private MotionContext _context;
        private List<BaseMotionHandler> _handlers = new();
        private Dictionary<Type, BaseMotionHandler> _handlersDict = new();

        // Observers
        private DesignPatterns.Observers.IObserver<float> _updateObserver;
        private DesignPatterns.Observers.IObserver<float> _lateUpdateObserver;
        private DesignPatterns.Observers.IObserver<float> _fixedUpdateObserver;
        private ActionObserver<bool> _onPaused;
        
        // RigidBody values
        private Vector3 _pauseVelocity;
        private RigidbodyConstraints _pauseConstrains;

        public MotionController(Rigidbody rigidBody, CapsuleCollider collider, Transform orientation, Transform look, MotionConfig[] handlerConfigs)
        {
            _rb = rigidBody;

            if (_rb.TryGet(out var rb))
            {
                _pauseConstrains = rb.constraints;
            }
            // _orientationTransform = orientation;
            // _lookTransform = look;

            _context = new MotionContext(rigidBody, collider, rigidBody.transform, look, orientation);
            BuildHandlers(handlerConfigs, false);
            RebuildHandlers();
            
            _onPaused = new ActionObserver<bool>(OnPauseHandler);

            PauseHandler.Attach(_onPaused);

            if (PauseHandler.IsPaused)
            {
                OnPauseHandler(true);
            }
        }

        public bool TryAddHandler<THandler>(THandler newHandler, bool rebuildHandlers = true) where THandler : BaseMotionHandler
        {
            if (newHandler == null) return false;

            var type = typeof(THandler);

            if (!_handlersDict.TryAdd(type, newHandler))
            {
#if UNITY_EDITOR
                Debug.LogWarning($"WARNING[MotionController] Handler of type {type} already exists. Skipping duplicate.");
#endif
                newHandler.Dispose();
                return false;
            }
            
            _handlers.Add(newHandler);

            if (rebuildHandlers)
            {
                RebuildHandlers();
            }
            
            return true;
        }

        public bool TryGetHandler<THandler>(out THandler handler) where THandler : BaseMotionHandler
        {
            handler = default;
            var type = typeof(THandler);

            if (!_handlersDict.TryGetValue(type, out var comp) || comp is not THandler castedHandler)
                return false;

            handler = castedHandler;
            return true;
        }

        public bool RemoveHandler<THandler>(bool dispose = true, bool rebuildHandlers = true)
        {
            var type = typeof(THandler);
            if (!_handlersDict.Remove(type, out var handler)) return false;
            _handlers.Remove(handler);

            if (rebuildHandlers)
            {
                RebuildHandlers();
            }
            
            if (dispose) handler.Dispose();
            return true;
        }

        public void AttachOnGrounded(DesignPatterns.Observers.IObserver<bool> onGrounded)
        {
            _context?.OnGroundedChanged.Attach(onGrounded);
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

        private void OnPauseHandler(bool paused)
        {
            if (!_rb.TryGet(out var rb)) return;
            
            if (paused)
            {
                _pauseVelocity = rb.velocity;
                _pauseConstrains = rb.constraints;
            
                rb.velocity = Vector3.zero;
                rb.constraints = RigidbodyConstraints.FreezeAll;
            
                rb.isKinematic = true;
            }
            else
            {
                rb.isKinematic = false;
            
                rb.constraints = _pauseConstrains;
                rb.velocity = _pauseVelocity;
            }
            
        }

        private void BuildHandlers(MotionConfig[] configs, in bool rebuildHandlers)
        {
            foreach (var config in configs)
            {
                if (config == null || !config.Enabled)
                    continue;

                config.AddHandler(this, rebuildHandlers);
            }
        }

        public void RebuildHandlers()
        {
            _handlers.Sort((a, b) => a.Order().CompareTo(b.Order()));
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
            
            Gizmos.color = Color.green;
            if (!_rb.TryGet(out var rb)) return;
            
            var velocity = rb.velocity;
            Gizmos.DrawRay(origin.position, velocity.normalized * 5);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(origin.position,$"V:{velocity.magnitude:0.00} X{velocity.x:0.00} Y{velocity.y:0.00} Z{velocity.z:0.00}");
#endif
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
            PauseHandler.Detach(_onPaused);
            
            _onPaused?.Dispose();
            _onPaused = null;
            
            foreach (var t in _handlers)
            {
                t?.Dispose();
            }
            
            _handlersDict?.Clear();
            _handlers?.Clear();
            _context?.Dispose();

            _handlers = null;
            _handlersDict = null;
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

        public bool StartDash()
        {
            //if (!TryGetHandler<DashHandler>(out var dash) || dash.IsDashing()) return false;
            
            _context.Dash = true;
            return true;
        }
    }
}