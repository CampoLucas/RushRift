using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Game.DesignPatterns.Observers;
using Game.Entities;
using Game.InputSystem;
using Game.Levels;
using Game.Saves;
using Game.UI;
using Game.Utils;
using MyTools.Global;
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

        private float _yaw;
        private float _pitch;
        private Vector2 _cachedDelta;
        private bool _active = true;
        
        private ActionObserver<float> _onSensibilityChanged;
        private ActionObserver<float> _onSmoothnessChanged;
        private ActionObserver<bool> _onLoading;
        private ActionObserver<Vector3, Vector3, Quaternion> _onPlayerSpawned;

        private NullCheck<Camera> _camera;
        private NullCheck<CinemachineBrain> _brain;

        protected override void Awake()
        {
            base.Awake();

            _onLoading = new ActionObserver<bool>(OnLoadingHandler);
            _onPlayerSpawned = new ActionObserver<Vector3, Vector3, Quaternion>(OnPlayerSpawnedHandler);
            
            GameEntry.LoadingState.AttachOnLoading(_onLoading, true);
            PlayerSpawner.PlayerSpawned.Attach(_onPlayerSpawned, true);
        }

        private void Start()
        {
            var saveData = SaveSystem.LoadSettings();
            sensibility = saveData.Camera.sensibility;
            smoothing = saveData.Camera.smoothness;

            _onSensibilityChanged = new ActionObserver<float>(OnSensibilityChanged);
            _onSmoothnessChanged = new ActionObserver<float>(OnSmoothnessChanged);


            if (Options.OnCameraSensibilityChanged == null)
            {
                Debug.Log("On Camara Sensibility is null");
            }
            
            Options.OnCameraSensibilityChanged?.Attach(_onSensibilityChanged);
            Options.OnCameraSmoothnessChanged?.Attach(_onSmoothnessChanged);
        }

        private void Update()
        {
            if (!_active || GameEntry.LoadingState.Loading) return;
            
#if false
            _cachedDelta = InputManager.GetValueVector(InputManager.LookInput);
#else    
            var rawDelta = InputManager.GetValueVector(InputManager.LookInput);
            _cachedDelta = Vector2.Lerp(_cachedDelta, rawDelta, Time.deltaTime * smoothing);
#endif
        }


        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (!_active || GameEntry.LoadingState.Loading) return;
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
        
        private void OnLoadingHandler(bool loading)
        {
            _active = !loading;

            if (loading)
            {
                // reset cached input so next frame starts clean
                _cachedDelta = Vector2.zero;
            }
            else
            {
                // once loading ends, reset rotation baseline
                Reset();
            }
        }

        private void Reset()
        {
            _yaw = 0f;
            _pitch = 0f;
            _cachedDelta = Vector2.zero;
        }
        
        private void OnPlayerSpawnedHandler(Vector3 position, Vector3 positionDifference, Quaternion rotation)
        {
            _active = false;
            var playerCheck = PlayerSpawner.Player;
            if (!_brain.TryGet(out var brain, GetBrain) || !playerCheck.TryGet(out var player))
            {
                this.Log(!_brain ? "Couldn't find the brain" : !playerCheck ? "Couldn't find the player" : "Unknown error", LogType.Error);

                _active = true;
                return;
            }
            
            //Reset();
            _yaw = rotation.eulerAngles.y;
            _pitch = rotation.eulerAngles.x;
            _cachedDelta = Vector2.zero;
            
            var brainTr = brain.transform;
            
            
            var eyesTr = player.Joints.GetJoint(EntityJoint.Eyes);
            //var prevState = brain.enabled;
            brain.enabled = true;

            var point = eyesTr.position - positionDifference;
            
            this.Log($"Teleport coords diff [{positionDifference}] point [{point}] pos [{position}]");
            
            brainTr.position = point;
            brainTr.rotation = rotation;
            brain.ManualUpdate(); // ensures brain & vcam sync once
            //brain.enabled = prevState;

            _active = true;
        }

        private Camera GetCamera()
        {
            return Camera.main;
        }

        private CinemachineBrain GetBrain()
        {
            if (_camera.TryGet(out var cam, GetCamera))
            {
                return cam.GetComponent<CinemachineBrain>();
            }

            return null;
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
            GameEntry.LoadingState.DetachOnLoading(_onLoading);
            PlayerSpawner.PlayerSpawned.Detach(_onPlayerSpawned);
            
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
