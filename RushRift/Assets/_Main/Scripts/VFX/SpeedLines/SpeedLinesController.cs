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
        private NullCheck<ActionObserver<bool>> _onPaused;
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
            if (!_onPaused)
            {
                _onPaused = new ActionObserver<bool>(OnPause);
            }

            PauseHandler.Attach(_onPaused.Get());
        }

        private void OnDisable()
        {
            PauseHandler.Detach(_onPaused.Get());
        }

        private void Update()
        {
            //if (effect.pause || _moveAmount == null) return;
            if (effect.pause || !_rigidbody) return;
            var on = data.SetEffect(_rigidbody.velocity.magnitude, effect) > 0;

            if (GlobalLevelManager.DashDamage && 
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

        private void OnPause(bool pause)
        {
            effect.pause = pause;
        }

        private void OnDestroy()
        {
            data = null;
            effect = null;
            targetEntity = null;
            _moveAmount = null;
            _onPaused.Dispose();
            _onPaused = null;
            _onUnpause?.Dispose();
            _onUnpause = null;
        }
    }
}
