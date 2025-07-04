using UnityEngine;
using Cinemachine;
using Game.Entities;
using Game.Entities.Components;

/// <summary>
/// 🎥 Dynamically adjusts Cinemachine FOV based on player forward speed.
/// </summary>
[AddComponentMenu("Player/Camera/FOV By Speed")]
[RequireComponent(typeof(CinemachineVirtualCamera))]
public class FOVController : MonoBehaviour
{
    #region Serialized Fields

    [Header("FOV Settings")]
    [Tooltip("Field of view at idle or low speed.")]
    [SerializeField, Range(30f, 120f)] private float startFOV = 70f;

    [Tooltip("Minimum FOV at max speed (more zoomed in).")]
    [SerializeField, Range(30f, 120f)] private float maxFOV = 60f;

    [Tooltip("How fast the FOV interpolates.")]
    [SerializeField, Range(0.1f, 50f)] private float lerpSpeed = 25f;

    [Header("🏃 Player Reference")]
    [Tooltip("Reference to the Player Controller script.")]
    [SerializeField] private PlayerController playerController;

    [Tooltip("Threshold forward input to trigger FOV change.")]
    [SerializeField, Range(0f, 1f)] private float forwardThreshold = 0.1f;

    #endregion

    #region Private Fields

    private CinemachineVirtualCamera virtualCamera;
    private float currentFOV;
    private float targetFOV;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        virtualCamera = GetComponent<CinemachineVirtualCamera>();
        currentFOV = startFOV;
        virtualCamera.m_Lens.FieldOfView = startFOV;
    }

    private void Update()
    {
        if (playerController == null || playerController.TryGetComponent<IMovement>(out var movement)) return;

        // Get current movement direction and orientation
        var velocity = movement.Velocity;
        var forward = playerController.Origin.forward;
        var speed = velocity.magnitude;

        // Measure how much of the movement is in the forward direction
        var forwardAmount = Vector3.Dot(velocity.normalized, forward);

        if (forwardAmount >= forwardThreshold && speed > 0.1f)
        {
            // Moving forward — lower FOV
            targetFOV = Mathf.Lerp(startFOV, maxFOV, speed / movement.BaseMaxSpeed);
        }
        else
        {
            // Not moving forward — reset to base FOV
            targetFOV = startFOV;
        }

        // Smoothly interpolate FOV
        currentFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * lerpSpeed);
        virtualCamera.m_Lens.FieldOfView = currentFOV;
    }

    #endregion
}