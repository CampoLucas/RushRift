using UnityEngine;

namespace _Main.Scripts.Entities._Player
{
    public class DashAbility : MonoBehaviour
    {
        [Header("Dash Components")]
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Transform cameraTransform;
        
        [Header("Dash Settings")]
        [SerializeField] private float dashDistance = 5f; // Total distance of dash
        [SerializeField] private float dashDuration = 0.3f;
        [SerializeField] private float dashCooldown = 1f;
        [SerializeField] private AnimationCurve dashSpeedCurve;

        private Vector3 _dashStartPosition;
        private Vector3 _dashEndPosition;
        private bool _isDashing;
        private float _dashStartTime;
        private float _nextDashTime;

        private void Start()
        {
            characterController = GetComponent<CharacterController>();
            if (!cameraTransform) cameraTransform = Camera.main.transform;
        }

        private void Update()
        {
            HandleDash();
        }

        private void HandleDash()
        {
            if (_isDashing)
            {
                var elapsed = Time.time - _dashStartTime;
                var progress = Mathf.Clamp01(elapsed / dashDuration);
                var curveValue = dashSpeedCurve.Evaluate(progress);

                // Lerp between start and end positions based on curve
                var currentPosition = Vector3.Lerp(_dashStartPosition, _dashEndPosition, curveValue);
                var moveVector = currentPosition - transform.position;
                
                characterController.Move(moveVector); // Move by the difference

                if (progress >= 1f)
                {
                    _isDashing = false;
                }
            }
            
            else if (Time.time >= _nextDashTime && Input.GetKeyDown(KeyCode.LeftShift))
            {
                StartDash();
            }
        }

        private void StartDash()
        {
            var forward = cameraTransform.forward;
            var right = cameraTransform.right;

            forward.y = 0;
            right.y = 0;

            var moveDirection = Vector3.zero;
            
            if (Input.GetKey(KeyCode.W)) moveDirection += forward;
            if (Input.GetKey(KeyCode.S)) moveDirection -= forward;
            if (Input.GetKey(KeyCode.D)) moveDirection += right;
            if (Input.GetKey(KeyCode.A)) moveDirection -= right;

            if (moveDirection != Vector3.zero)
            {
                moveDirection.Normalize();
                
                _dashStartPosition = transform.position;
                _dashEndPosition = _dashStartPosition + moveDirection * dashDistance;

                _isDashing = true;
                _dashStartTime = Time.time;
                _nextDashTime = Time.time + dashCooldown;
            }
        }
    }
}