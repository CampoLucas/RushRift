using System;
using Game.DesignPatterns.Observers;
using Game.Entities;
using Game.Entities.Components;
using Game.Entities.Components.MotionController;
using Game.UI;
using MyTools.Global;
using UnityEngine;
using UnityEngine.VFX;

namespace Game.VFX
{
    public class SpeedLinesController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private VisualEffect effect;
        
        [Header("Settings")]
        [SerializeField] private SpeedLinesData data;

        [SerializeField] private Gradient normalGradient;
        [SerializeField] private Gradient dashDamageGradient;

        private NullCheck<PlayerController> _targetEntity;
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
            _targetEntity = PlayerSpawner.Player;
            
            if (_targetEntity.TryGet(out var player))
            {
                if (player.GetModel().TryGetComponent<IMovement>(out var movement))
                {
                    _moveAmount = movement.MoveAmount;
                }

                if (player.TryGetComponent<Rigidbody>(out var rb))
                {
                    _rigidbody = rb;
                }
            }
            else
            {
                this.Log("Target Entity not found.", LogType.Error);
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
            OnPause(PauseHandler.IsPaused);

        }

        private void OnDisable()
        {
            PauseHandler.Detach(_onPaused.Get());
        }

        private void Update()
        {
            //if (effect.pause || _moveAmount == null) return;
            if (effect.pause || !_rigidbody)
            {
                return;
            }
            
            var velocity = _rigidbody.velocity;
            var speed = velocity.magnitude;
            var on = data.SetEffect(speed, effect) > 0;

            // --- new section: apply offset based on velocity ---
            if (speed > 0.1f)
            {
                // Normalize velocity to get direction
                var dir = velocity.normalized;

                // Offset backwards relative to direction
                var offset = -dir * data.PositionOffsetAmount; // add this float to SpeedLinesData

                // Set it to the VFX Graph
                effect.SetVector3("Position", offset);
            }
            else
            {
                // When stopped, reset
                effect.SetVector3("Position", Vector3.zero);
            }
            
            if (GlobalLevelManager.DashDamage && _targetEntity.TryGet(out var player) &&
                player.GetModel().TryGetComponent<MotionController>(out var controller) &&
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
            _targetEntity = null;
            _moveAmount = null;
            _onPaused.Dispose();
            _onPaused = null;
            _onUnpause?.Dispose();
            _onUnpause = null;
        }
    }
}
