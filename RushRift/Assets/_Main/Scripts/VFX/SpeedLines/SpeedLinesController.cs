using System;
using Game.DesignPatterns.Observers;
using Game.Entities;
using Game.Entities.Components;
using Game.Entities.Components.MotionController;
using Game.UI;
using UnityEngine;
using UnityEngine.VFX;

namespace Game.VFX
{
    public class SpeedLinesController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private VisualEffect effect;
        [SerializeField] private EntityController targetEntity;
        
        [Header("Settings")]
        [SerializeField] private SpeedLinesData data;

        [SerializeField] private Gradient normalGradient;
        [SerializeField] private Gradient dashDamageGradient;
        
        
        private Func<float> _moveAmount;
        private bool _started;
        private IObserver _onPaused;
        private IObserver _onUnpause;
        private Rigidbody _rigidbody;
        private bool _dashing;

        private void Awake()
        {
            effect.Stop();
        }

        private void Start()
        {
            if (targetEntity.GetModel().TryGetComponent<IMovement>(out var movement))
            {
                _moveAmount = movement.MoveAmount;
            }

            if (targetEntity && targetEntity.TryGetComponent<Rigidbody>(out var rb))
            {
                _rigidbody = rb;
            }
            
            effect.SetGradient("ColorRadiant", normalGradient);
        }
        
        private void OnEnable()
        {
            if (_onPaused == null)
            {
                _onPaused = new ActionObserver(OnPause);
            }

            if (_onUnpause == null)
            {
                _onUnpause = new ActionObserver(OnUnpause);
            }

            var onPaused = UIManager.OnPaused;
            var onUnpause = UIManager.OnUnpaused;
            if (onPaused != null)
            {
                onPaused.Attach(_onPaused);
            }

            if (onUnpause != null)
            {
                onUnpause.Attach(_onUnpause);
            }
        }

        private void OnDisable()
        {
            var onPaused = UIManager.OnPaused;
            var onUnpause = UIManager.OnUnpaused;
            if (onPaused != null)
            {
                onPaused.Detach(_onPaused);
            }

            if (onUnpause != null)
            {
                onUnpause.Detach(_onUnpause);
            }
        }

        private void Update()
        {
            //if (effect.pause || _moveAmount == null) return;
            if (effect.pause || !_rigidbody) return;
            var on = data.SetEffect(_rigidbody.velocity.magnitude, effect) > 0;

            var hasDashDamage = LevelManager.HasDashDamage;
            
            if (hasDashDamage && 
                targetEntity.GetModel().TryGetComponent<MotionController>(out var controller) &&
                controller.TryGetHandler<DashHandler>(out var handler))
            {
                if (!_dashing && handler.IsDashing)
                {
                    _dashing = true;
                    effect.SetGradient("ColorRadiant", dashDamageGradient);
                }

                if (_dashing && !handler.IsDashing)
                {
                    _dashing = false;
                    effect.SetGradient("ColorRadiant", normalGradient);
                }
            }
            
            if (on && !_started)
            {
                _started = true;
                effect.Play();
            }
            
            if (!on && _started)
            {
                _started = false;
                effect.Stop();
            }
        }

        private void OnPause()
        {
            effect.pause = true;
        }

        private void OnUnpause()
        {
            effect.pause = false;
        }

        private void OnDestroy()
        {
            data = null;
            effect = null;
            targetEntity = null;
            _moveAmount = null;
            _onPaused?.Dispose();
            _onPaused = null;
            _onUnpause?.Dispose();
            _onUnpause = null;
        }
    }
}
