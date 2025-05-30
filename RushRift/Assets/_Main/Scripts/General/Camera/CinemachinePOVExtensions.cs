using System;
using UnityEngine;
using Cinemachine;
using Game.Inputs;

namespace Game
{
    public class CinemachinePOVExtensions : CinemachineExtension
    {
        [Header("Settings")]
        [SerializeField] private float clampAngle = 70;
        [SerializeField] private bool invertHorizontal;
        [SerializeField] private bool invertVertical;
        
        [Header("Speed")]
        [SerializeField] private float sensivility = .3f;
        [SerializeField] private float smoothing = 10f;
        // [SerializeField] private float horizontalSpeed = 10;
        // [SerializeField] private float verticalSpeed = 10;
        //private bool _startedRotating;

        private float _yaw;
        private float _pitch;
        private Vector2 _cachedDelta;

        private void Update()
        {
#if false
            _cachedDelta = InputManager.GetValueVector(InputManager.LookInput);
#else    
            var rawDelta = InputManager.GetValueVector(InputManager.LookInput);

            _cachedDelta = Vector2.Lerp(_cachedDelta, rawDelta, Time.deltaTime * smoothing);
#endif
        }


        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (vcam.Follow == null || stage != CinemachineCore.Stage.Aim) return;

#if false
            var deltaInput = InputManager.GetValueVector(InputManager.LookInput);
#else
            var deltaInput = _cachedDelta;
#endif

            if (invertHorizontal) deltaInput.x *= -1;
            if (invertVertical) deltaInput.y *= -1;
            
            Debug.Log($"Mouse input: {deltaInput.magnitude}");

            _yaw += deltaInput.x * sensivility;
            _pitch += deltaInput.y * sensivility;
            _pitch = Mathf.Clamp(_pitch, -clampAngle, clampAngle);

            var rotation = Quaternion.Euler(-_pitch, _yaw, 0f);
            state.RawOrientation = rotation;
        }
    }
}
