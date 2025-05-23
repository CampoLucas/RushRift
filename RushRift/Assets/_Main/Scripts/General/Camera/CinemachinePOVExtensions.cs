using System;
using UnityEngine;
using Cinemachine;
using Game.Inputs;

namespace Game
{
    public class CinemachinePOVExtensions : CinemachineExtension
    {
        [Header("Settings")]
        [SerializeField] private bool invertHorizontal;
        [SerializeField] private bool invertVertical;
        [SerializeField] private float clampAngle = 70;
        
        [Header("Speed")]
        [SerializeField] private float sensivility;
        [SerializeField] private float horizontalSpeed = 10;
        [SerializeField] private float verticalSpeed = 10;
        
        
        private Transform _transform;
        private Vector3 _startRotation;
        //private bool _startedRotating;

        private float _yaw;
        private float _pitch;

        private void Start()
        {
            _transform = transform;
            _startRotation = _transform.localRotation.eulerAngles;
        }

        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
#if false
            if (vcam.Follow == null || stage != CinemachineCore.Stage.Aim) return;
            
            var deltaInput = InputManager.GetValueVector(InputManager.LookInput);
                    
            Debug.Log($"Mouse input: {deltaInput.magnitude}");

            var xInput = deltaInput.x * horizontalSpeed * sensivility * Time.deltaTime;
            if (invertHorizontal) xInput *= -1;
            var yInput = deltaInput.y * verticalSpeed * sensivility * Time.deltaTime;
            if (invertVertical) yInput *= -1;
                    
                    
            _startRotation.x += xInput;
            _startRotation.y -= yInput;

            _startRotation.y = Mathf.Clamp(_startRotation.y, -clampAngle, clampAngle);
            state.RawOrientation = Quaternion.Euler(_startRotation.y, _startRotation.x, 0f);
#else
            if (vcam.Follow == null || stage != CinemachineCore.Stage.Aim) return;
            
            var deltaInput = InputManager.GetValueVector(InputManager.LookInput);

            if (invertHorizontal) deltaInput.x *= -1;
            if (invertVertical) deltaInput.y *= -1;
            
            Debug.Log($"Mouse input: {deltaInput.magnitude}");

            _yaw += deltaInput.x * sensivility;
            _pitch += deltaInput.y * sensivility;
            _pitch = Mathf.Clamp(_pitch, -clampAngle, clampAngle);

            var rotation = Quaternion.Euler(-_pitch, _yaw, 0f);
            state.RawOrientation = rotation;
#endif
        }
    }
}
