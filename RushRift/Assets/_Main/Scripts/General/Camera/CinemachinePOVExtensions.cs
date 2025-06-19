using UnityEngine;
using Cinemachine;
using Game.DesignPatterns.Observers;
using Game.Inputs;
using Game.UI;
using UnityEngine.Serialization;

namespace Game
{
    public class CinemachinePOVExtensions : CinemachineExtension
    {
        [Header("Settings")]
        [SerializeField] private float clampAngle = 70;
        [SerializeField] private bool invertHorizontal;
        [SerializeField] private bool invertVertical;
        
        [Header("Speed")]
        [SerializeField] private float sensibility = .3f;
        [SerializeField] private float smoothing = 10f;
        // [SerializeField] private float horizontalSpeed = 10;
        // [SerializeField] private float verticalSpeed = 10;
        //private bool _startedRotating;

        private float _yaw;
        private float _pitch;
        private Vector2 _cachedDelta;
        private ActionObserver<float> _onSensibilityChanged;
        private ActionObserver<float> _onSmoothnessChanged;

        private void Start()
        {
            var saveData = SaveAndLoad.Load();

            sensibility = saveData.camera.Sensibility;
            smoothing = saveData.camera.Smoothness;

            _onSensibilityChanged = new ActionObserver<float>(OnSensibilityChanged);
            _onSmoothnessChanged = new ActionObserver<float>(OnSmoothnessChanged);


            if (Options.OnCameraSensibilityChanged == null)
            {
                Debug.Log("On Camara Sensibility is null");
            }
            Options.OnCameraSensibilityChanged.Attach(_onSensibilityChanged);
            Options.OnCameraSmoothnessChanged.Attach(_onSmoothnessChanged);
        }

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
            
            _yaw += deltaInput.x * sensibility;
            _pitch += deltaInput.y * sensibility;
            _pitch = Mathf.Clamp(_pitch, -clampAngle, clampAngle);

            var rotation = Quaternion.Euler(-_pitch, _yaw, 0f);
            state.RawOrientation = rotation;
        }

        private void OnSensibilityChanged(float value)
        {
            sensibility = value;
        }

        private void OnSmoothnessChanged(float value)
        {
            smoothing = value;
        }

        protected override void OnDestroy()
        {
            var sensibilitySubject = Options.OnCameraSensibilityChanged;
            var smoothnessSubject = Options.OnCameraSensibilityChanged;

            if (_onSensibilityChanged != null)
            {
                if (sensibilitySubject != null) sensibilitySubject.Detach(_onSensibilityChanged);
                _onSensibilityChanged.Dispose();
            }

            if (_onSmoothnessChanged != null)
            {
                if (smoothnessSubject != null) smoothnessSubject.Detach(_onSmoothnessChanged);
                _onSmoothnessChanged?.Dispose();
            }
        }
    }
}
