using UnityEngine;

/// <summary>
/// 🎮 Handles player movement using CharacterController with smooth acceleration, jumping, and air control.
/// </summary>
[AddComponentMenu("Player/Player Movement")]
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    #region Serialized Fields

    [Header("Input Settings")]
    [Tooltip("Transform representing movement direction (e.g. camera forward/right).")]
    [SerializeField] private Transform orientation;

    [Header("Movement Settings")]
    [Tooltip("Maximum movement speed.")]
    [SerializeField] private float maxSpeed = 8f;

    [Tooltip("Acceleration rate while moving.")]
    [SerializeField] private float acceleration = 10f;

    [Tooltip("Deceleration rate when no input.")]
    [SerializeField] private float deceleration = 20f;

    [Tooltip("Curve controlling acceleration behavior.")]
    [SerializeField] private AnimationCurve accelerationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("Curve controlling deceleration behavior.")]
    [SerializeField] private AnimationCurve decelerationCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Air Settings")]
    [Tooltip("Multiplier controlling movement influence in the air (0 = no control, 1 = full control).")]
    [SerializeField, Range(0f, 1f)] private float airControl = 0.5f;

    [Tooltip("Gravity applied when in the air.")]
    [SerializeField] private float gravity = -20f;

    [Tooltip("Initial force applied when jumping.")]
    [SerializeField] private float jumpForce = 10f;

    [Header("Ground Check")]
    [Tooltip("Transform used for ground check origin.")]
    [SerializeField] private Transform groundCheckPoint;

    [Tooltip("Ground check radius.")]
    [SerializeField] private float groundCheckRadius = 0.3f;

    [Tooltip("Layers considered ground.")]
    [SerializeField] private LayerMask groundMask;

    #endregion

    #region Private Fields

    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 moveDirection;
    private float accelerationTimer;
    private float decelerationTimer;
    private bool isGrounded;
    private bool jumpRequested;

    #endregion

    #region Unity Events

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        CheckGrounded();
        HandleInput();
        ApplyMovement(Time.deltaTime);
        
        Vector3 cameraEuler = Camera.main.transform.eulerAngles;
        orientation.rotation = Quaternion.Euler(0, cameraEuler.y, 0);

    }

    #endregion

    #region Input Handling

    private void HandleInput()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(x, 0, z).normalized;
        
        Vector3 camForward = orientation.forward;
        Vector3 camRight = orientation.right;
        
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();
        
        moveDirection = (camForward * input.z + camRight * input.x).normalized;

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            jumpRequested = true;
        }
    }

    #endregion

    #region Movement Logic

    private void ApplyMovement(float deltaTime)
    {
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
        Vector3 targetVelocity = moveDirection * maxSpeed;

        // Reduce control while airborne
        float moveMultiplier = isGrounded ? 1f : airControl;
        targetVelocity *= moveMultiplier;

        Vector3 velocityDelta = targetVelocity - horizontalVelocity;

        float rate = moveDirection.magnitude > 0.01f ? acceleration : deceleration;
        float curveFactor = moveDirection.magnitude > 0.01f
            ? accelerationCurve.Evaluate(Mathf.Clamp01(accelerationTimer += deltaTime))
            : decelerationCurve.Evaluate(Mathf.Clamp01(decelerationTimer += deltaTime));

        if (moveDirection.magnitude <= 0.01f) accelerationTimer = 0;
        else decelerationTimer = 0;

        velocityDelta = Vector3.ClampMagnitude(velocityDelta, rate * deltaTime * curveFactor);
        horizontalVelocity += velocityDelta;

        // Jumping
        if (jumpRequested)
        {
            velocity.y = jumpForce;
            jumpRequested = false;
        }

        // Apply gravity
        velocity.y += gravity * deltaTime;

        // Combine final velocity and move
        velocity = new Vector3(horizontalVelocity.x, velocity.y, horizontalVelocity.z);
        controller.Move(velocity * deltaTime);

        // Reset vertical velocity if grounded
        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }
    }

    #endregion

    #region Ground Check

    private void CheckGrounded()
    {
        isGrounded = groundCheckPoint != null
            ? Physics.CheckSphere(groundCheckPoint.position, groundCheckRadius, groundMask)
            : controller.isGrounded;
    }

    #endregion
}
