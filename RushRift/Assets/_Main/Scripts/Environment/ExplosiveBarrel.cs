using System.Collections;
using System.Collections.Generic;
using Game;
using Game.Entities;
using Game.Entities.Components;
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

    [Header("Health Integration")]
    [SerializeField, Tooltip("Health data used to construct and register the HealthComponent on the Entity Model.")]
    private HealthComponentData healthComponentData;

    [SerializeField, Tooltip("If enabled, maps HealthComponentData.OnZeroHealth to the post explosion action.")]
    private bool useDieBehaviourForPostAction = true;

    [Header("Explosion Settings")]
    [SerializeField, Tooltip("If enabled, the barrel triggers its explosion when this GameObject is destroyed at runtime.")]
    private bool shouldExplodeOnDestroy = true;

    [SerializeField, Tooltip("Explosion origin offset in local space (added to this Transform.position).")]
    private Vector3 explosionOriginLocalOffset = Vector3.zero;

    [SerializeField, Tooltip("Explosion radius in meters for detecting targets.")]
    private float explosionRadiusMeters = 6f;

    [SerializeField, Tooltip("Optional delay in seconds before executing the explosion logic once triggered.")]
    private float explosionDelaySeconds;

    [Header("Distance-Based Damage")]
    [SerializeField, Tooltip("Maximum damage dealt at zero distance.")]
    private float explosionDamageMax = 100f;

    [SerializeField, Tooltip("Minimum damage dealt at the edge of the radius.")]
    private float explosionDamageMin = 10f;

    [Header("Distance-Based Impulse")]
    [SerializeField, Tooltip("Maximum launch speed for the player at zero distance (m/s).")]
    private float playerLaunchSpeedMaxMetersPerSecond = 18f;

    [SerializeField, Tooltip("Minimum launch speed for the player at the edge of the radius (m/s).")]
    private float playerLaunchSpeedMinMetersPerSecond = 6f;

    [SerializeField, Tooltip("Maximum AddExplosionForce applied to non-player rigidbodies at zero distance.")]
    private float otherRigidbodiesExplosionImpulseMax = 20f;

    [SerializeField, Tooltip("Minimum AddExplosionForce applied to non-player rigidbodies at the edge of the radius.")]
    private float otherRigidbodiesExplosionImpulseMin = 4f;

    [SerializeField, Tooltip("Upwards modifier used by AddExplosionForce for non-player rigidbodies.")]
    private float otherRigidbodiesUpwardsModifier = 0.5f;

    [Header("Post Explosion Action")]
    [SerializeField, Tooltip("What to do with this barrel after the explosion finishes.")]
    private PostExplosionAction postExplosionAction = PostExplosionAction.DestroyGameObject;

    [SerializeField, Tooltip("Optional extra delay in seconds before performing the post-explosion action.")]
    private float postExplosionActionDelaySeconds;

    [Header("Player Target")]
    [SerializeField, Tooltip("Tag used to identify the Player object.")]
    private string requiredPlayerTag = "Player";

    [SerializeField, Tooltip("How the launch direction is determined.")]
    private LaunchDirectionSource launchDirectionSource = LaunchDirectionSource.RadialFromBarrelCenter;

    [SerializeField, Tooltip("Used when LaunchDirectionSource is CustomVector. Will be normalized; Vector3.zero falls back to world up.")]
    private Vector3 customLaunchDirection = Vector3.up;

    [SerializeField, Tooltip("Used when LaunchDirectionSource is ReferenceTransformForward/Up.")]
    private Transform directionReferenceTransform;

    [SerializeField, Tooltip("If true, zeroes sideways velocity relative to the launch direction; otherwise preserves it.")]
    private bool shouldResetTangentialVelocity;

    [Header("Enemy Targeting")]
    [SerializeField, Tooltip("Tag used to identify Enemy objects. Leave empty to affect any EntityController that is not the player.")]
    private string enemyTag = "Enemy";

    [Header("Medal Condition")]
    [SerializeField, Tooltip("If true, overrides the global upgrade gate, otherwise the barrel reads LevelManager.BarrelInvulnerabilityEnabled.")]
    private bool overrideMedalConditionLocally;

    [SerializeField, Tooltip("Local medal condition used only when Override is enabled.")]
    private bool localMedalConditionAchieved;

    [Header("Debug")]
    [SerializeField, Tooltip("If enabled, prints detailed debug logs.")]
    private bool isDebugLoggingEnabled;

    [SerializeField, Tooltip("If enabled, draws gizmos for the explosion radius and example launch direction.")]
    private bool drawGizmos = true;

    private static readonly Collider[] OverlapBuffer = new Collider[128];
    private bool hasExplosionAlreadyTriggered;
    private bool applicationIsQuitting;
    private bool explosionInitiatedByOnDestroy;

    private EntityController cachedEntityController;
    private IModel cachedModel;
    private HealthComponent runtimeHealthComponent;
    private Coroutine ensureRoutine;

    private void Awake()
    {
        ResolveControllerAndModel();
        EnsureHealthRegisteredImmediateOrDeferred();
        SyncPostActionFromDieBehaviour();
        if (explosionDelaySeconds > 0f && hasExplosionAlreadyTriggered) Invoke(nameof(ExecuteExplosionNow), explosionDelaySeconds);
    }

    private void OnEnable()
    {
        ResolveControllerAndModel();
        EnsureHealthRegisteredImmediateOrDeferred();
    }

    private void OnTransformParentChanged()
    {
        ResolveControllerAndModel();
        EnsureHealthRegisteredImmediateOrDeferred();
    }

    private void OnDisable()
    {
        if (ensureRoutine != null)
        {
            StopCoroutine(ensureRoutine);
            ensureRoutine = null;
        }
    }

    private void OnApplicationQuit() => applicationIsQuitting = true;

    private void Update()
    {
        if (!hasExplosionAlreadyTriggered && runtimeHealthComponent != null)
        {
            if (!runtimeHealthComponent.IsAlive() || runtimeHealthComponent.Value <= 0f)
            {
                hasExplosionAlreadyTriggered = true;
                ExecuteExplosionNow();
            }
        }
    }

    private void OnDestroy()
    {
        if (!Application.isPlaying) return;
        if (shouldExplodeOnDestroy && !hasExplosionAlreadyTriggered && !applicationIsQuitting)
        {
            explosionInitiatedByOnDestroy = true;
            hasExplosionAlreadyTriggered = true;
            ExecuteExplosionNow();
        }
    }

    private void ExecuteExplosionNow()
    {
        Vector3 explosionOriginWorld = transform.TransformPoint(explosionOriginLocalOffset);
        int overlappedCount = Physics.OverlapSphereNonAlloc(explosionOriginWorld, Mathf.Max(0f, explosionRadiusMeters), OverlapBuffer, ~0, QueryTriggerInteraction.Collide);

        var processedControllers = new HashSet<EntityController>();
        var processedRoots = new HashSet<Transform>();

        for (int i = 0; i < overlappedCount; i++)
        {
            var col = OverlapBuffer[i];
            if (!col) continue;

            var controller = col.GetComponentInParent<EntityController>();
            if (controller && processedControllers.Contains(controller)) continue;

            var root = col.transform.root;
            if (processedRoots.Contains(root)) continue;

            Vector3 closest = col.ClosestPoint(explosionOriginWorld);
            float distance = Vector3.Distance(explosionOriginWorld, closest);
            float closeness = Mathf.Clamp01(1f - Mathf.InverseLerp(0f, Mathf.Max(0.0001f, explosionRadiusMeters), distance));

            float scaledDamage = Mathf.Lerp(explosionDamageMin, explosionDamageMax, closeness);
            float scaledPlayerSpeed = Mathf.Lerp(playerLaunchSpeedMinMetersPerSecond, playerLaunchSpeedMaxMetersPerSecond, closeness);
            float scaledOtherImpulse = Mathf.Lerp(otherRigidbodiesExplosionImpulseMin, otherRigidbodiesExplosionImpulseMax, closeness);

            GameObject targetObject = controller ? controller.Origin.gameObject : root.gameObject;
            bool isPlayer = !string.IsNullOrEmpty(requiredPlayerTag) && targetObject.CompareTag(requiredPlayerTag);
            bool isEnemyByTag = !string.IsNullOrEmpty(enemyTag) && targetObject.CompareTag(enemyTag);
            bool isEnemyByType = controller is EnemyController;
            bool isEnemy = isEnemyByTag || isEnemyByType;

            if (controller) processedControllers.Add(controller); else processedRoots.Add(root);

            if (isPlayer)
            {
                if (targetObject.TryGetComponent<Rigidbody>(out var rb))
                {
                    ApplyJumpPadStyleLaunch(rb, explosionOriginWorld, true, Mathf.Max(0f, scaledPlayerSpeed));
                }
                if (!IsMedalConditionActive())
                {
                    TryDealScaledDamageOrNotify(targetObject, "Player", explosionOriginWorld, Mathf.Max(0f, scaledDamage));
                }
                LogDebug($"Player hit | dist={distance:0.##} closeness={closeness:0.##} dmg={scaledDamage:0.##} speed={scaledPlayerSpeed:0.##}");
            }
            else
            {
                TryDealScaledDamageOrNotify(targetObject, isEnemy ? "Enemy" : "Entity", explosionOriginWorld, Mathf.Max(0f, scaledDamage));

                if (targetObject.TryGetComponent<Rigidbody>(out var otherRb) && scaledOtherImpulse > 0f)
                {
                    otherRb.AddExplosionForce(scaledOtherImpulse, explosionOriginWorld, explosionRadiusMeters, otherRigidbodiesUpwardsModifier, ForceMode.Impulse);
                }
                LogDebug($"Target hit | name={targetObject.name} dist={distance:0.##} closeness={closeness:0.##} dmg={scaledDamage:0.##} impulse={scaledOtherImpulse:0.##}");
            }
        }

        if (!explosionInitiatedByOnDestroy)
        {
            if (postExplosionActionDelaySeconds > 0f) StartCoroutine(DelayedPostExplosionAction(postExplosionActionDelaySeconds));
            else DoPostExplosionAction();
        }

        LogDebug($"Explosion executed | origin={explosionOriginWorld} radius={explosionRadiusMeters} medalActive={IsMedalConditionActive()}");
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
                break;
            case PostExplosionAction.DeactivateGameObject:
                gameObject.SetActive(false);
                break;
            case PostExplosionAction.DestroyGameObject:
                Destroy(gameObject);
                break;
        }
    }

    private void ApplyJumpPadStyleLaunch(Rigidbody targetRigidbody, Vector3 explosionOriginWorld, bool isPlayer, float targetSpeed)
    {
        Vector3 launchDirectionWorld = ComputeLaunchDirection(targetRigidbody, explosionOriginWorld, isPlayer);
        Vector3 v = targetRigidbody.velocity;
        float along = Vector3.Dot(v, launchDirectionWorld);
        Vector3 tangential = v - launchDirectionWorld * along;
        if (shouldResetTangentialVelocity) tangential = Vector3.zero;
        targetRigidbody.velocity = tangential + launchDirectionWorld * Mathf.Max(0f, targetSpeed);
        LogDebug($"Launch applied | target={targetRigidbody.name} dir={launchDirectionWorld} speed={targetSpeed:0.##}");
    }

    private Vector3 ComputeLaunchDirection(Rigidbody targetRigidbody, Vector3 explosionOriginWorld, bool isPlayer)
    {
        if (isPlayer && IsMedalConditionActive()) return Vector3.up;

        Vector3 dir;
        switch (launchDirectionSource)
        {
            case LaunchDirectionSource.BarrelUp: dir = transform.up; break;
            case LaunchDirectionSource.BarrelForward: dir = transform.forward; break;
            case LaunchDirectionSource.RadialFromBarrelCenter: dir = targetRigidbody.worldCenterOfMass - explosionOriginWorld; break;
            case LaunchDirectionSource.CustomVector: dir = customLaunchDirection; break;
            case LaunchDirectionSource.ReferenceTransformForward: dir = directionReferenceTransform ? directionReferenceTransform.forward : transform.forward; break;
            case LaunchDirectionSource.ReferenceTransformUp: dir = directionReferenceTransform ? directionReferenceTransform.up : transform.up; break;
            default: dir = Vector3.up; break;
        }

        if (dir.sqrMagnitude < 1e-6f) dir = Vector3.up;
        return dir.normalized;
    }

    private void TryDealScaledDamageOrNotify(GameObject targetObject, string label, Vector3 hitPosition, float scaledDamage)
    {
        if (!targetObject) return;

        if (targetObject.TryGetComponent<EntityController>(out var controller))
        {
            var model = controller.GetModel();
            if (model != null && model.TryGetComponent<HealthComponent>(out var health))
            {
                health.Damage(scaledDamage, hitPosition);
                LogDebug($"{label} damaged via HealthComponent.Damage | name={targetObject.name} amount={scaledDamage:0.##}");
                return;
            }

            controller.OnNotify(EntityController.DESTROY);
            LogDebug($"{label} destroyed via EntityController observer | name={targetObject.name}");
            return;
        }

        LogDebug($"{label} has no EntityController | name={targetObject.name}");
    }

    private void ResolveControllerAndModel()
    {
        cachedEntityController = GetComponentInParent<EntityController>();
        if (!cachedEntityController) cachedEntityController = GetComponent<EntityController>();
        cachedModel = cachedEntityController != null ? cachedEntityController.GetModel() : null;
    }

    private void EnsureHealthRegisteredImmediateOrDeferred()
    {
        if (runtimeHealthComponent != null) return;

        if (cachedModel != null)
        {
            if (!cachedModel.TryGetComponent(out runtimeHealthComponent))
            {
                if (healthComponentData != null)
                {
                    runtimeHealthComponent = new HealthComponent(healthComponentData);
                    cachedModel.TryAddComponent(runtimeHealthComponent);
                }
            }
            SyncPostActionFromDieBehaviour();
            if (runtimeHealthComponent != null) LogDebug($"Health ready | value={runtimeHealthComponent.Value:0.##}");
        }
        else
        {
            if (ensureRoutine == null) ensureRoutine = StartCoroutine(EnsureHealthRegisteredDeferred());
        }
    }

    private IEnumerator EnsureHealthRegisteredDeferred()
    {
        int safetyFrames = 16;
        while (safetyFrames-- > 0 && runtimeHealthComponent == null)
        {
            ResolveControllerAndModel();
            if (cachedModel != null)
            {
                EnsureHealthRegisteredImmediateOrDeferred();
                if (runtimeHealthComponent != null) break;
            }
            yield return null;
        }
        ensureRoutine = null;
    }

    private void SyncPostActionFromDieBehaviour()
    {
        if (!useDieBehaviourForPostAction || healthComponentData == null) return;
        switch (healthComponentData.OnZeroHealth)
        {
            case DieBehaviour.Nothing: postExplosionAction = PostExplosionAction.None; break;
            case DieBehaviour.Destroy: postExplosionAction = PostExplosionAction.DestroyGameObject; break;
            case DieBehaviour.Disable: postExplosionAction = PostExplosionAction.DeactivateGameObject; break;
        }
    }

    private bool IsMedalConditionActive()
    {
        return overrideMedalConditionLocally ? localMedalConditionAchieved : LevelManager.BarrelInvulnerabilityEnabled;
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

        Vector3 exampleDirection = Vector3.up;
        switch (launchDirectionSource)
        {
            case LaunchDirectionSource.BarrelUp: exampleDirection = transform.up; break;
            case LaunchDirectionSource.BarrelForward: exampleDirection = transform.forward; break;
            case LaunchDirectionSource.RadialFromBarrelCenter: exampleDirection = Vector3.up; break;
            case LaunchDirectionSource.CustomVector: exampleDirection = (customLaunchDirection.sqrMagnitude < 1e-6f) ? Vector3.up : customLaunchDirection.normalized; break;
            case LaunchDirectionSource.ReferenceTransformForward: exampleDirection = directionReferenceTransform ? directionReferenceTransform.forward : transform.forward; break;
            case LaunchDirectionSource.ReferenceTransformUp: exampleDirection = directionReferenceTransform ? directionReferenceTransform.up : transform.up; break;
        }

        Gizmos.color = IsMedalConditionActive() ? Color.cyan : Color.yellow;
        Gizmos.DrawRay(origin, exampleDirection.normalized * Mathf.Min(1.0f, explosionRadiusMeters * 0.5f));
    }
}