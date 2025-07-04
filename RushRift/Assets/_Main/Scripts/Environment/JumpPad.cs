using Game.Entities;
using Game.Entities.Components;
using UnityEngine;

/// <summary>
/// 🟩 Launches the player when they enter the trigger zone.
/// </summary>
[AddComponentMenu("Player/Environment/Jump Pad")]
[RequireComponent(typeof(Collider))]
public class JumpPad : MonoBehaviour
{
    #region Serialized Fields

    [Header("Launch Settings")]
    [Tooltip("Vertical force applied to the player.")]
    [SerializeField, Range(5f, 1000f)] private float launchForce = 20f;

    [Tooltip("Extra forward push applied based on pad direction.")]
    [SerializeField, Range(0f, 20f)] private float forwardBoost;

    [Tooltip("Launch direction override. Uses transform.up by default.")]
    [SerializeField] private Vector3 launchDirection = Vector3.up;

    #endregion

    #region Unity Methods

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (other.TryGetComponent(out IController controller) && controller.GetModel().TryGetComponent<IMovement>(out var movement))
        {
            // Get pad direction
            Vector3 direction = transform.TransformDirection(launchDirection.normalized);
            Vector3 impulse = direction * launchForce;

            // Add optional horizontal boost
            if (forwardBoost > 0)
            {
                impulse += transform.forward * forwardBoost;
            }

            movement.ApplyImpulse(impulse);
        }
    }

    #endregion
}