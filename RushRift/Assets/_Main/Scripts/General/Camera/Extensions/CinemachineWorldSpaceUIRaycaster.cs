using Cinemachine;
using Game.InputSystem;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game
{
    [DefaultExecutionOrder(1000)] // run after camera update
    [RequireComponent(typeof(CinemachineBrain))]
    public class CinemachineWorldSpaceUIRaycaster : CinemachineExtension
    {
        [Header("UI Interaction")]
        [SerializeField] private float rayDistance = 5f;
        [SerializeField] private LayerMask uiLayerMask = ~0; // default: all layers
        [SerializeField] private bool debugRay = false;
        
        private Transform _camera;
        private EventSystem _eventSystem;
        
        protected override void Awake()
        {
            base.Awake();
            _camera = GetComponent<CinemachineBrain>().OutputCamera.transform;
            _eventSystem = EventSystem.current;

            if (_eventSystem == null)
                Debug.LogWarning("⚠ No EventSystem found! UI clicks won’t work.");
        }

        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            // Only handle logic after camera position is finalized
            if (stage != CinemachineCore.Stage.Finalize) return;
            if (_camera == null || _eventSystem == null) return;
            
            // Optional: debug ray
            if (debugRay)
            {
                Debug.DrawRay(_camera.position, _camera.forward * rayDistance, Color.cyan);
            }

            // Left click to interact
            if (InputManager.GetActionPerformed(InputManager.PrimaryAttackInput))
            {
                var ray = new Ray(_camera.position, _camera.forward);

                if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, uiLayerMask))
                {
                    // Create pointer data at screen center
                    PointerEventData pointer = new PointerEventData(_eventSystem)
                    {
                        position = new Vector2(Screen.width / 2f, Screen.height / 2f)
                    };

                    // Try to click the UI element
                    ExecuteEvents.Execute(hit.transform.gameObject, pointer, ExecuteEvents.pointerClickHandler);
                }
            }
        }
    }
}