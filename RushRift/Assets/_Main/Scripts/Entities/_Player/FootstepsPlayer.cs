using Game;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FootstepsPlayer : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string footstepSoundName = "Footsteps";  // Must match AudioManager's Sound name
    [SerializeField] private float minVelocityThreshold = 0.2f;      // Minimum speed to trigger steps
    [SerializeField] private float stepInterval = 0.5f;              // Time between steps

    [Header("Ground Check (optional)")]
    [SerializeField] private bool requireGrounded = true;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody rb;
    private float stepTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        float speed = rb.velocity.magnitude;

        if (speed >= minVelocityThreshold && (!requireGrounded || IsGrounded()))
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                AudioManager.Play(footstepSoundName);
                stepTimer = stepInterval;
            }
        }
        else
        {
            stepTimer = 0f; // Reset timer when not moving or not grounded
        }
    }

    private bool IsGrounded()
    {
        if (groundCheck == null)
            return true; // Assume always grounded if not specified

        return Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}