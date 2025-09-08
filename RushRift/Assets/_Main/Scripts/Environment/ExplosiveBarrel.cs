using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class ExplosiveBarrel : MonoBehaviour
{
    public enum LaunchDirectionSource
    {
        BarrelUp,
        BarrelForward,
        RadialFromBarrelCenter,
        CustomVector,
        ReferenceTransformForward,
        ReferenceTransformUp
    }

    public enum PostExplosionAction
    {
        None,
        DeactivateGameObject,
        DestroyGameObject
    }

    [Header("Explosion Settings")]
    [SerializeField, Tooltip("If enabled, the barrel triggers its explosion when this GameObject is destroyed at runtime.")]
    private bool shouldExplodeOnDestroy = true;

    [SerializeField, Tooltip("Explosion origin offset in local space (added to this Transform.position).")]
    private Vector3 explosionOriginLocalOffset = Vector3.zero;

    [SerializeField, Tooltip("Explosion radius in meters for detecting the player (and optional rigidbodies).")]
    private float explosionRadiusMeters = 6f;

    [SerializeField, Tooltip("Optional delay in seconds before executing the explosion logic once triggered. Ignored when exploding from OnDestroy (object is already being destroyed).")]
    private float explosionDelaySeconds = 0f;

    [Header("After Explosion Action")]
    [SerializeField, Tooltip("What to do with this barrel after the explosion finishes (when triggered via TriggerExplosion/ExecuteExplosionNow). On OnDestroy-driven explosions, the object is already being destroyed.")]
    private PostExplosionAction postExplosionAction = PostExplosionAction.DestroyGameObject;

    [SerializeField, Tooltip("Optional extra delay in seconds before performing the post-explosion action (Deactivate/Destroy).")]
    private float postExplosionActionDelaySeconds = 0f;

    [Header("Player Launch")]
    [SerializeField, Tooltip("Tag that the launched object must have. Leave as 'Player' for the main character.")]
    private string requiredPlayerTag = "Player";

    [SerializeField, Tooltip("How fast the player will be launched along the computed direction (m/s).")]
    private float playerLaunchSpeedMetersPerSecond = 14f;

    [SerializeField, Tooltip("How the launch direction is determined.")]
    private LaunchDirectionSource launchDirectionSource = LaunchDirectionSource.RadialFromBarrelCenter;

    [SerializeField, Tooltip("Used when LaunchDirectionSource is CustomVector. Will be normalized; Vector3.zero falls back to world up.")]
    private Vector3 customLaunchDirection = Vector3.up;

    [SerializeField, Tooltip("Used when LaunchDirectionSource is ReferenceTransformForward/Up.")]
    private Transform directionReferenceTransform;

    [SerializeField, Tooltip("If true, zeroes sideways velocity relative to the launch direction; otherwise preserves it.")]
    private bool shouldResetTangentialVelocity = false;

    [Header("Optional Physics For Others")]
    [SerializeField, Tooltip("If > 0, applies Unity's AddExplosionForce to any non-player rigidbodies within radius.")]
    private float otherRigidbodiesExplosionImpulse = 0f;

    [SerializeField, Tooltip("Upwards modifier used by AddExplosionForce for non-player rigidbodies.")]
    private float otherRigidbodiesUpwardsModifier = 0.5f;

    [Header("Debug")]
    [SerializeField, Tooltip("If enabled, prints detailed debug logs.")]
    private bool isDebugLoggingEnabled = false;

    [SerializeField, Tooltip("If enabled, draws gizmos for the explosion radius and example launch direction.")]
    private bool drawGizmos = true;

    private static readonly Collider[] OverlapBuffer = new Collider[64];
    private bool hasExplosionAlreadyTriggered;
    private bool applicationIsQuitting;
    private bool explosionInitiatedByOnDestroy; // tracks whether explosion started from OnDestroy (object is already being destroyed)

    private void OnApplicationQuit() => applicationIsQuitting = true;

    // (Debug hotkey was in your file; keep or remove as you wish)
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            TriggerExplosion();
        }
    }

    private void OnDestroy()
    {
        if (!Application.isPlaying) return;

        if (shouldExplodeOnDestroy && !hasExplosionAlreadyTriggered && !applicationIsQuitting)
        {
            // Since we are already being destroyed, run the explosion immediately (ignore explosionDelaySeconds)
            explosionInitiatedByOnDestroy = true;
            hasExplosionAlreadyTriggered = true;
            ExecuteExplosionNow();
        }
    }

    /// <summary>Call this to make the barrel explode now (respects explosionDelaySeconds).</summary>
    public void TriggerExplosion()
    {
        if (hasExplosionAlreadyTriggered) return;
        hasExplosionAlreadyTriggered = true;

        if (explosionDelaySeconds > 0f)
            Invoke(nameof(ExecuteExplosionNow), explosionDelaySeconds);
        else
            ExecuteExplosionNow();
    }

    private void ExecuteExplosionNow()
    {
        Vector3 explosionOriginWorld = transform.TransformPoint(explosionOriginLocalOffset);

        // 1) Overlap scan
        int count = Physics.OverlapSphereNonAlloc(
            explosionOriginWorld,
            Mathf.Max(0f, explosionRadiusMeters),
            OverlapBuffer,
            ~0,
            QueryTriggerInteraction.Collide
        );

        // Track which rigidbodies we already handled (a single object may have multiple colliders)
        var processedRigidbodies = new HashSet<Rigidbody>();

        // 2) Affect nearby things
        for (int i = 0; i < count; i++)
        {
            var col = OverlapBuffer[i];
            if (!col) continue;

            var rb = col.attachedRigidbody ? col.attachedRigidbody : col.GetComponentInParent<Rigidbody>();
            if (!rb) continue;
            if (processedRigidbodies.Contains(rb)) continue;
            processedRigidbodies.Add(rb);

            GameObject rootObject = rb.gameObject;

            // 2a) Launch the player with JumpPad-style velocity set
            if (string.IsNullOrEmpty(requiredPlayerTag) || rootObject.CompareTag(requiredPlayerTag))
            {
                ApplyJumpPadStyleLaunch(rb, explosionOriginWorld);
                continue;
            }

            // 2b) Optionally push other rigidbodies using explosion force
            if (otherRigidbodiesExplosionImpulse > 0f)
            {
                rb.AddExplosionForce(
                    otherRigidbodiesExplosionImpulse,
                    explosionOriginWorld,
                    explosionRadiusMeters,
                    otherRigidbodiesUpwardsModifier,
                    ForceMode.Impulse
                );
            }
        }

        LogDebug($"Explosion at {explosionOriginWorld} | radius={explosionRadiusMeters}");

        // 3) Perform post-explosion action if we were NOT triggered by OnDestroy
        if (!explosionInitiatedByOnDestroy)
        {
            if (postExplosionActionDelaySeconds > 0f)
                StartCoroutine(DelayedPostExplosionAction(postExplosionActionDelaySeconds));
            else
                DoPostExplosionAction();
        }

        // Optional: play VFX/SFX here (ParticleSystem, AudioSource, etc.)
    }

    private IEnumerator DelayedPostExplosionAction(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        DoPostExplosionAction();
    }

    private void DoPostExplosionAction()
    {
        switch (postExplosionAction)
        {
            case PostExplosionAction.None:
                // do nothing
                break;

            case PostExplosionAction.DeactivateGameObject:
                gameObject.SetActive(false);
                break;

            case PostExplosionAction.DestroyGameObject:
                Destroy(gameObject);
                break;
        }
    }

    private void ApplyJumpPadStyleLaunch(Rigidbody targetRigidbody, Vector3 explosionOriginWorld)
    {
        Vector3 launchDirectionWorld = ComputeLaunchDirection(targetRigidbody, explosionOriginWorld);

        Vector3 currentVelocity = targetRigidbody.velocity;
        float currentAlong = Vector3.Dot(currentVelocity, launchDirectionWorld);
        Vector3 tangentialComponent = currentVelocity - launchDirectionWorld * currentAlong;

        if (shouldResetTangentialVelocity)
            tangentialComponent = Vector3.zero;

        Vector3 newVelocity =
            tangentialComponent +
            launchDirectionWorld * Mathf.Max(0f, playerLaunchSpeedMetersPerSecond);

        targetRigidbody.velocity = newVelocity;

        LogDebug($"Launched {targetRigidbody.name} dir={launchDirectionWorld} speed={playerLaunchSpeedMetersPerSecond:0.##} oldVel={currentVelocity} newVel={newVelocity}");
    }

    private Vector3 ComputeLaunchDirection(Rigidbody targetRigidbody, Vector3 explosionOriginWorld)
    {
        Vector3 direction;

        switch (launchDirectionSource)
        {
            case LaunchDirectionSource.BarrelUp:
                direction = transform.up;
                break;

            case LaunchDirectionSource.BarrelForward:
                direction = transform.forward;
                break;

            case LaunchDirectionSource.RadialFromBarrelCenter:
                direction = (targetRigidbody.worldCenterOfMass - explosionOriginWorld);
                break;

            case LaunchDirectionSource.CustomVector:
                direction = customLaunchDirection;
                break;

            case LaunchDirectionSource.ReferenceTransformForward:
                direction = directionReferenceTransform ? directionReferenceTransform.forward : transform.forward;
                break;

            case LaunchDirectionSource.ReferenceTransformUp:
                direction = directionReferenceTransform ? directionReferenceTransform.up : transform.up;
                break;

            default:
                direction = Vector3.up;
                break;
        }

        if (direction.sqrMagnitude < 1e-6f) direction = Vector3.up;
        return direction.normalized;
    }

    private void LogDebug(string message)
    {
        if (!isDebugLoggingEnabled) return;
        Debug.Log($"[ExplosiveBarrel] {name}: {message}", this);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Vector3 origin = transform.TransformPoint(explosionOriginLocalOffset);

        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.85f);
        Gizmos.DrawWireSphere(origin, Mathf.Max(0f, explosionRadiusMeters));

        // Representative direction arrow
        Vector3 exampleDirection = Vector3.up;
        switch (launchDirectionSource)
        {
            case LaunchDirectionSource.BarrelUp: exampleDirection = transform.up; break;
            case LaunchDirectionSource.BarrelForward: exampleDirection = transform.forward; break;
            case LaunchDirectionSource.RadialFromBarrelCenter: exampleDirection = Vector3.up; break; // depends on target
            case LaunchDirectionSource.CustomVector: exampleDirection = (customLaunchDirection.sqrMagnitude < 1e-6f) ? Vector3.up : customLaunchDirection.normalized; break;
            case LaunchDirectionSource.ReferenceTransformForward: exampleDirection = directionReferenceTransform ? directionReferenceTransform.forward : transform.forward; break;
            case LaunchDirectionSource.ReferenceTransformUp: exampleDirection = directionReferenceTransform ? directionReferenceTransform.up : transform.up; break;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(origin, exampleDirection.normalized * Mathf.Min(1.0f, explosionRadiusMeters * 0.5f));
    }
}
