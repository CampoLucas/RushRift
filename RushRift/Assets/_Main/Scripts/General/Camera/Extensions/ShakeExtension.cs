using System;
using Cinemachine;
using Game.DesignPatterns.Observers;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game
{
    public class ShakeExtension : CinemachineExtension
    {
        private ActionObserver<float, float> _shakeObserver;
        
        [SerializeField] private Vector3 magnitudeScale = Vector3.one;
        
        private float _elapsed;
        private float _duration;
        private float _magnitude;
        private bool _shaking;
        private bool _started;

        protected override void Awake()
        {
            base.Awake();

            _shakeObserver = new ActionObserver<float, float>(Shake);
        }

        private void Start()
        {
            // Subscribes to CameraManager's shake events
            EffectManager.AttachShake(_shakeObserver);
        }


        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam, 
            CinemachineCore.Stage stage, 
            ref CameraState state, 
            float deltaTime)
        {
            if (!_shaking) return;

            // Apply shake only in the Body stage (position modifications)
            if (stage != CinemachineCore.Stage.Body) return;

            _elapsed -= deltaTime;

            if (_elapsed <= 0)
            {
                _shaking = false;
                return;
            }
            
            // Calculate a shake factor
            var damper = _elapsed / _duration; // optional fade out
            var shakeOffset = Random.insideUnitSphere * _magnitude * damper;
            shakeOffset.x *= magnitudeScale.x;
            shakeOffset.y *= magnitudeScale.y;
            shakeOffset.z *= magnitudeScale.z;
            
            // Apply the offset to the camera position
            state.PositionCorrection += shakeOffset;
        }

        private void Shake(float duration, float magnitude)
        {
            _shaking = true;
            _duration = duration;
            _elapsed = duration;
            _magnitude = magnitude;
        }

        protected override void OnDestroy()
        {
            EffectManager.DetachShake(_shakeObserver);
            _shakeObserver.Dispose();
            _shakeObserver = null;
        }
    }
}