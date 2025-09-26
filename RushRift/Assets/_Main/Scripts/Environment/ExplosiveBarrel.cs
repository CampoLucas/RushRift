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

    public enum ExplosionZone
    {
        Outside,
        Outer,
        Inner
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

    [SerializeField, Tooltip("Global detection radius used to collect colliders. Should be >= Outer Radius.")]
    private float explosionDetectionRadiusMeters = 8f;

    [Header("Dual Area Settings")]
    [SerializeField, Tooltip("Inner damage radius in meters.")]
    private float innerRadiusMeters = 3f;

    [SerializeField, Tooltip("Outer damage radius in meters (must be >= inner).")]
    private float outerRadiusMeters = 6f;

    [Header("Player Impact: Damage")]
    [SerializeField, Tooltip("Damage applied to player when inside Inner radius.")]
    private float playerDamageInnerAmount = 100f;

    [SerializeField, Tooltip("Damage applied to player when inside Outer radius but outside Inner.")]
    private float playerDamageOuterAmount = 50f;

    [Header("Player Impact: Launch Speed")]
    [SerializeField, Tooltip("Launch speed applied to player in Inner radius (m/s).")]
    private float playerLaunchSpeedInnerMetersPerSecond = 18f;

    [SerializeField, Tooltip("Launch speed applied to player in Outer radius (m/s).")]
    private float playerLaunchSpeedOuterMetersPerSecond = 12f;

    [Header("Post Explosion Action")]
    [SerializeField, Tooltip("What to do with this barrel after the explosion finishes.")]
    private PostExplosionAction postExplosionAction = PostExplosionAction.DestroyGameObject;

    [SerializeField, Tooltip("Optional extra delay in seconds before performing the post-explosion action.")]
    private float postExplosionActionDelaySeconds = 0f;

    [Header("Player Target")]
    [SerializeField, Tooltip("Tag used to identify the Player object.")]
    private string requiredPlayerTag = "Player";

    [SerializeField, Tooltip("How the launch direction is determined for non-medal behavior.")]
    private LaunchDirectionSource launchDirectionSource = LaunchDirectionSource.RadialFromBarrelCenter;

    [SerializeField, Tooltip("Used when LaunchDirectionSource is CustomVector. Will be normalized; Vector3.zero falls back to world up.")]
    private Vector3 customLaunchDirection = Vector3.up;

    [SerializeField, Tooltip("Used when LaunchDirectionSource is ReferenceTransformForward/Up.")]
    private Transform directionReferenceTransform;

    [SerializeField, Tooltip("If true, zeroes sideways velocity relative to the launch direction; otherwise preserves it.")]
    private bool shouldResetTangentialVelocity = false;

    [Header("Enemy Targeting")]
    [SerializeField, Tooltip("Tag used to identify Enemy objects. Leave empty to affect any EntityController that is not the player.")]
    private string enemyTag = "Enemy";

    [Header("Medal Condition")]
    [SerializeField, Tooltip("If true, overrides the global upgrade gate, otherwise the barrel reads LevelManager.BarrelInvulnerabilityEnabled.")]
    private bool overrideMedalConditionLocally = false;

    [SerializeField, Tooltip("Local medal condition used only when Override is enabled.")]
    private bool localMedalConditionAchieved = false;

    [Header("Optional Physics For Others")]
    [SerializeField, Tooltip("If > 0, applies Unity's AddExplosionForce to any non-player rigidbodies within detection radius.")]
    private float otherRigidbodiesExplosionImpulse = 0f;

    [SerializeField, Tooltip("Upwards modifier used by AddExplosionForce for non-player rigidbodies.")]
    private float otherRigidbodiesUpwardsModifier = 0.5f;

    [Header("Debug")]
    [SerializeField, Tooltip("If enabled, prints detailed debug logs.")]
    private bool isDebugLoggingEnabled = false;

    [SerializeField, Tooltip("If enabled, draws gizmos for the explosion radii and example launch directions.")]
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

    [ContextMenu("Trigger Explosion Now")]
    public void TriggerExplosion()
    {
        if (hasExplosionAlreadyTriggered) return;
        hasExplosionAlreadyTriggered = true;
        ExecuteExplosionNow();
    }

    private void ExecuteExplosionNow()
    {
        float clampedInner = Mathf.Max(0f, innerRadiusMeters);
        float clampedOuter = Mathf.Max(clampedInner, outerRadiusMeters);
        float detectionRadius = Mathf.Max(clampedOuter, explosionDetectionRadiusMeters);

        Vector3 explosionOriginWorld = transform.TransformPoint(explosionOriginLocalOffset);
        int overlappedCount = Physics.OverlapSphereNonAlloc(
            explosionOriginWorld,
            detectionRadius,
            OverlapBuffer,
            ~0,
            QueryTriggerInteraction.Collide
        );

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

            GameObject targetObject = controller ? controller.Origin.gameObject : root.gameObject;
            bool isPlayer = !string.IsNullOrEmpty(requiredPlayerTag) && targetObject.CompareTag(requiredPlayerTag);
            bool isEnemyByTag = !string.IsNullOrEmpty(enemyTag) && targetObject.CompareTag(enemyTag);
            bool isEnemyByType = controller is EnemyController;
            bool isEnemy = isEnemyByTag || isEnemyByType;

            if (controller) processedControllers.Add(controller); else processedRoots.Add(root);

            if (isPlayer)
            {
                Rigidbody playerRigidbody = null;
                targetObject.TryGetComponent(out playerRigidbody);

                float distance = Vector3.Distance(explosionOriginWorld, playerRigidbody ? playerRigidbody.worldCenterOfMass : targetObject.transform.position);
                ExplosionZone zone = ClassifyZone(distance, clampedInner, clampedOuter);

                if (zone != ExplosionZone.Outside)
                {
                    float zoneSpeed = zone == ExplosionZone.Inner ? playerLaunchSpeedInnerMetersPerSecond : playerLaunchSpeedOuterMetersPerSecond;
                    if (playerRigidbody) ApplyLaunchWithSpeed(playerRigidbody, explosionOriginWorld, zoneSpeed, true);

                    if (!IsMedalConditionActive())
                    {
                        float dmg = zone == ExplosionZone.Inner ? playerDamageInnerAmount : playerDamageOuterAmount;
                        TryDamagePlayerViaHealthOrNotify(targetObject, explosionOriginWorld, dmg);
                    }
                }
            }
            else
            {
                TryKillViaHealthOrNotify(targetObject, isEnemy ? "Enemy" : "Entity", explosionOriginWorld);

                if (otherRigidbodiesExplosionImpulse > 0f && targetObject.TryGetComponent<Rigidbody>(out var otherRb))
                {
                    otherRb.AddExplosionForce(
                        otherRigidbodiesExplosionImpulse,
                        explosionOriginWorld,
                        detectionRadius,
                        otherRigidbodiesUpwardsModifier,
                        ForceMode.Impulse
                    );
                }
            }
        }

        if (!explosionInitiatedByOnDestroy)
        {
            if (postExplosionActionDelaySeconds > 0f)
                StartCoroutine(DelayedPostExplosionAction(postExplosionActionDelaySeconds));
            else
                DoPostExplosionAction();
        }

        LogDebug($"Explosion executed | origin={explosionOriginWorld} inner={clampedInner} outer={clampedOuter} medalActive={IsMedalConditionActive()}");
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

    private void ApplyLaunchWithSpeed(Rigidbody targetRigidbody, Vector3 explosionOriginWorld, float launchSpeed, bool isPlayer)
    {
        Vector3 launchDirectionWorld = ComputeLaunchDirection(targetRigidbody, explosionOriginWorld, isPlayer);
        Vector3 v = targetRigidbody.velocity;
        float along = Vector3.Dot(v, launchDirectionWorld);
        Vector3 tangential = v - launchDirectionWorld * along;
        if (shouldResetTangentialVelocity) tangential = Vector3.zero;
        targetRigidbody.velocity = tangential + launchDirectionWorld * Mathf.Max(0f, launchSpeed);
        LogDebug($"Launch applied | target={targetRigidbody.name} dir={launchDirectionWorld} speed={launchSpeed:0.##}");
    }

    private Vector3 ComputeLaunchDirection(Rigidbody targetRigidbody, Vector3 explosionOriginWorld, bool isPlayer)
    {
        if (isPlayer && IsMedalConditionActive()) return Vector3.up;

        Vector3 dir;
        switch (launchDirectionSource)
        {
            case LaunchDirectionSource.BarrelUp: dir = transform.up; break;
            case LaunchDirectionSource.BarrelForward: dir = transform.forward; break;
            case LaunchDirectionSource.RadialFromBarrelCenter: dir = targetRigidbody ? (targetRigidbody.worldCenterOfMass - explosionOriginWorld) : (transform.position - explosionOriginWorld); break;
            case LaunchDirectionSource.CustomVector: dir = customLaunchDirection; break;
            case LaunchDirectionSource.ReferenceTransformForward: dir = directionReferenceTransform ? directionReferenceTransform.forward : transform.forward; break;
            case LaunchDirectionSource.ReferenceTransformUp: dir = directionReferenceTransform ? directionReferenceTransform.up : transform.up; break;
            default: dir = Vector3.up; break;
        }

        if (dir.sqrMagnitude < 1e-6f) dir = Vector3.up;
        return dir.normalized;
    }

    private void TryDamagePlayerViaHealthOrNotify(GameObject playerObject, Vector3 hitPosition, float damageAmount)
    {
        if (!playerObject.TryGetComponent<EntityController>(out var controller))
        {
            LogDebug("Player missing EntityController");
            return;
        }

        var model = controller.GetModel();
        if (model != null && model.TryGetComponent<HealthComponent>(out var health))
        {
            if (damageAmount >= float.MaxValue * 0.5f)
            {
                health.Intakill(hitPosition);
                LogDebug($"Player killed via Intakill");
            }
            else
            {
                health.Damage(Mathf.Max(0f, damageAmount), hitPosition);
                LogDebug($"Player damaged | amount={damageAmount:0.##}");
            }
            return;
        }

        controller.OnNotify(EntityController.DESTROY);
        LogDebug("Player destroyed via EntityController observer");
    }

    private void TryKillViaHealthOrNotify(GameObject targetObject, string label, Vector3 hitPosition)
    {
        if (!targetObject) return;

        if (targetObject.TryGetComponent<EntityController>(out var controller))
        {
            var model = controller.GetModel();
            if (model != null && model.TryGetComponent<HealthComponent>(out var health))
            {
                health.Intakill(hitPosition);
                LogDebug($"{label} killed via HealthComponent.Intakill | name={targetObject.name}");
                return;
            }

            controller.OnNotify(EntityController.DESTROY);
            LogDebug($"{label} destroyed via EntityController observer | name={targetObject.name}");
            return;
        }

        LogDebug($"{label} has no EntityController | name={targetObject.name}");
    }

    private ExplosionZone ClassifyZone(float distance, float inner, float outer)
    {
        if (distance <= inner) return ExplosionZone.Inner;
        if (distance <= outer) return ExplosionZone.Outer;
        return ExplosionZone.Outside;
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

        float clampedInner = Mathf.Max(0f, innerRadiusMeters);
        float clampedOuter = Mathf.Max(clampedInner, outerRadiusMeters);
        float detectionRadius = Mathf.Max(clampedOuter, explosionDetectionRadiusMeters);

        Vector3 origin = transform.TransformPoint(explosionOriginLocalOffset);

        Gizmos.color = new Color(1f, 0.35f, 0.25f, 0.9f);
        Gizmos.DrawWireSphere(origin, clampedInner);

        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(origin, clampedOuter);

        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(origin, detectionRadius);

        Vector3 exampleDir = Vector3.up;
        switch (launchDirectionSource)
        {
            case LaunchDirectionSource.BarrelUp: exampleDir = transform.up; break;
            case LaunchDirectionSource.BarrelForward: exampleDir = transform.forward; break;
            case LaunchDirectionSource.CustomVector: exampleDir = (customLaunchDirection.sqrMagnitude < 1e-6f) ? Vector3.up : customLaunchDirection.normalized; break;
            case LaunchDirectionSource.ReferenceTransformForward: exampleDir = directionReferenceTransform ? directionReferenceTransform.forward : transform.forward; break;
            case LaunchDirectionSource.ReferenceTransformUp: exampleDir = directionReferenceTransform ? directionReferenceTransform.up : transform.up; break;
        }

        Gizmos.color = IsMedalConditionActive() ? Color.cyan : Color.yellow;
        Gizmos.DrawRay(origin, exampleDir.normalized * Mathf.Min(1.0f, clampedInner));
    }
}