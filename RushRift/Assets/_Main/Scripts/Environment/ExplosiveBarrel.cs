using System.Collections;
using System.Collections.Generic;
using Game;
using Game.Entities;
using Game.Entities.Components;
using UnityEngine;
using Game.Entities.Components.MotionController;
using Game.VFX;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class ExplosiveBarrel : MonoBehaviour
{
    [Header("Health Integration")]
    [SerializeField, Tooltip("Health data used to construct and register the HealthComponent on the Entity Model.")]
    private HealthComponentData healthComponentData;

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

    [Header("Tags")]
    [SerializeField, Tooltip("Tag used to identify the Player object.")]
    private string requiredPlayerTag = "Player";

    [SerializeField, Tooltip("Tag used to identify Enemy objects. Leave empty to affect any EntityController that is not the player.")]
    private string enemyTag = "Enemy";

    [Header("External Triggering")]
    [SerializeField, Tooltip("If enabled, external scripts can trigger the explosion explicitly.")]
    private bool allowExternalExplosionTrigger = true;

    [Header("Audio")]
    [SerializeField, Tooltip("If enabled, plays an audio event when the barrel explodes.")]
    private bool shouldPlayExplosionAudio = true;

    [SerializeField, Tooltip("Audio event name passed to AudioManager.Play on explosion.")]
    private string explosionAudioEventName = "BarrelExplosion";

    [Header("Visual Effects")]
    [SerializeField, Tooltip("Visual Effect Graph asset spawned at the explosion point.")]
    private VFXPrefabID explosionVFX;

    [SerializeField, Tooltip("Local offset from the explosion origin where the VFX will be placed.")]
    private Vector3 explosionVfxLocalOffset = Vector3.zero;

    [Header("Chain Reaction")]
    [SerializeField, Tooltip("Extra delay added only when this barrel is detonated by another barrel's explosion.")]
    private float chainReactionDelaySeconds = 0.12f;

    [Header("Gizmos")]
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

    private class AggregatedHit
    {
        public Transform Group;
        public EntityController Controller;
        public Rigidbody Rigidbody;
        public float MinDistance;
        public bool HasDistance;
    }

    private void Awake()
    {
        ResolveControllerAndModel();
        EnsureHealthRegisteredImmediateOrDeferred();

        if (explosionDelaySeconds > 0f && hasExplosionAlreadyTriggered)
            Invoke(nameof(ExecuteExplosionNow), explosionDelaySeconds);
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
                if (explosionDelaySeconds > 0f) StartCoroutine(DelayedExplosion(explosionDelaySeconds));
                else ExecuteExplosionNow();
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
            if (explosionDelaySeconds > 0f) StartCoroutine(DelayedExplosion(explosionDelaySeconds));
            else ExecuteExplosionNow();
        }
    }

    private IEnumerator DelayedExplosion(float delay)
    {
        yield return new WaitForSeconds(delay);
        ExecuteExplosionNow();
    }

    private void ExecuteExplosionNow()
    {
        Vector3 origin = transform.TransformPoint(explosionOriginLocalOffset);
        ExecuteExplosionAtOrigin(origin, false, null);
    }

    private void ExecuteExplosionAtOrigin(Vector3 origin, bool forceMaxImpulseForPlayerAndRigidbodies, Rigidbody guaranteedImpulseTarget)
    {
        TriggerExplosionAudioVfx(origin);

        int count = Physics.OverlapSphereNonAlloc(origin, Mathf.Max(0f, explosionRadiusMeters), OverlapBuffer, ~0, QueryTriggerInteraction.Collide);
        var byGroup = new Dictionary<Transform, AggregatedHit>(count);

        for (int i = 0; i < count; i++)
        {
            var col = OverlapBuffer[i];
            if (!col) continue;

            var controller = col.GetComponentInParent<EntityController>();
            Transform group = controller ? controller.Origin.transform : (col.attachedRigidbody ? col.attachedRigidbody.transform : col.transform);

            if (!byGroup.TryGetValue(group, out var agg))
            {
                agg = new AggregatedHit { Group = group, MinDistance = float.PositiveInfinity };
                byGroup.Add(group, agg);
            }

            if (agg.Controller == null && controller) agg.Controller = controller;

            if (!agg.Rigidbody)
            {
                var rb = col.attachedRigidbody ? col.attachedRigidbody : group.GetComponent<Rigidbody>();
                if (rb) agg.Rigidbody = rb;
            }

            Vector3 closest = col.ClosestPoint(origin);
            float d = Vector3.Distance(origin, closest);
            if (d < agg.MinDistance)
            {
                agg.MinDistance = d;
                agg.HasDistance = true;
            }
        }

        if (guaranteedImpulseTarget)
        {
            Transform group = guaranteedImpulseTarget.transform;
            if (!byGroup.TryGetValue(group, out var agg))
            {
                agg = new AggregatedHit { Group = group, MinDistance = 0f, HasDistance = true, Rigidbody = guaranteedImpulseTarget };
                byGroup.Add(group, agg);
            }
            else
            {
                agg.Rigidbody = guaranteedImpulseTarget;
                agg.MinDistance = 0f;
                agg.HasDistance = true;
            }
        }

        foreach (var kv in byGroup)
        {
            var agg = kv.Value;
            float distance = agg.HasDistance ? agg.MinDistance : Vector3.Distance(origin, agg.Group.position);
            float closeness = Mathf.Clamp01(1f - Mathf.InverseLerp(0f, Mathf.Max(0.0001f, explosionRadiusMeters), distance));

            float scaledDamage = Mathf.Lerp(explosionDamageMin, explosionDamageMax, closeness);

            float impulseT = forceMaxImpulseForPlayerAndRigidbodies ? 1f : closeness;
            float scaledPlayerSpeed = Mathf.Lerp(playerLaunchSpeedMinMetersPerSecond, playerLaunchSpeedMaxMetersPerSecond, impulseT);
            float scaledOtherImpulse = Mathf.Lerp(otherRigidbodiesExplosionImpulseMin, otherRigidbodiesExplosionImpulseMax, impulseT);

            GameObject targetObject = agg.Controller ? agg.Controller.Origin.gameObject : agg.Group.gameObject;

            bool isEnemyByTag = !string.IsNullOrEmpty(enemyTag) && targetObject.CompareTag(enemyTag);
            bool isEnemyByType = agg.Controller is EnemyController;
            bool isEnemy = isEnemyByTag || isEnemyByType;

            bool isPlayer = IsPlayerObject(agg.Group) || (agg.Controller is PlayerController);

            var otherBarrel = agg.Group.GetComponentInParent<ExplosiveBarrel>();

            if (otherBarrel && otherBarrel != this)
            {
                HealthComponent otherHealth = null;
                if (agg.Controller)
                {
                    var mdl = agg.Controller.GetModel();
                    if (mdl != null) mdl.TryGetComponent(out otherHealth);
                }

                if (otherHealth != null)
                {
                    float wouldRemain = otherHealth.Value - Mathf.Max(0f, scaledDamage);
                    if (wouldRemain <= 0f)
                    {
                        otherBarrel.TriggerExplosionExternal(null, false, null, Mathf.Max(0f, chainReactionDelaySeconds));
                        continue;
                    }
                }
            }

            if (isPlayer)
            {
                var rb = agg.Rigidbody ? agg.Rigidbody : agg.Group.GetComponentInParent<Rigidbody>();

                if (rb)
                {
                    ApplyPlayerLaunch(rb, Mathf.Max(0f, scaledPlayerSpeed));
                }
                else if (agg.Controller != null)
                {
                    var model = agg.Controller.GetModel();
                    if (model != null && model.TryGetComponent<MotionController>(out var motion))
                    {
                        var dir = transform.up;
                        motion.ExternalImpulse(dir * scaledPlayerSpeed);
                    }
                }

                if (!IsMedalConditionActive())
                    TryDealScaledDamageOrNotify(targetObject, "Player", origin, Mathf.Max(0f, scaledDamage));
            }
            else
            {
                TryDealScaledDamageOrNotify(targetObject, isEnemy ? "Enemy" : "Entity", origin, Mathf.Max(0f, scaledDamage));

                if (agg.Rigidbody && scaledOtherImpulse > 0f)
                    agg.Rigidbody.AddExplosionForce(scaledOtherImpulse, origin, explosionRadiusMeters, otherRigidbodiesUpwardsModifier, ForceMode.Impulse);
            }
        }

        if (!explosionInitiatedByOnDestroy)
            gameObject.SetActive(false);
    }

    public void TriggerExplosionExternal(Vector3? overrideWorldOrigin = null, bool forceMaxImpulseForPlayerAndRigidbodies = false, Rigidbody guaranteedImpulseTarget = null, float delaySeconds = 0f)
    {
        if (!allowExternalExplosionTrigger) return;
        if (hasExplosionAlreadyTriggered) return;

        hasExplosionAlreadyTriggered = true;
        explosionInitiatedByOnDestroy = false;

        if (delaySeconds > 0f)
        {
            StartCoroutine(DelayedExternal(overrideWorldOrigin, forceMaxImpulseForPlayerAndRigidbodies, guaranteedImpulseTarget, delaySeconds));
            return;
        }

        Vector3 origin = overrideWorldOrigin ?? transform.TransformPoint(explosionOriginLocalOffset);
        ExecuteExplosionAtOrigin(origin, forceMaxImpulseForPlayerAndRigidbodies, guaranteedImpulseTarget);
    }

    private IEnumerator DelayedExternal(Vector3? overrideWorldOrigin, bool forceMaxImpulseForPlayerAndRigidbodies, Rigidbody guaranteedImpulseTarget, float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        Vector3 origin = overrideWorldOrigin ?? transform.TransformPoint(explosionOriginLocalOffset);
        ExecuteExplosionAtOrigin(origin, forceMaxImpulseForPlayerAndRigidbodies, guaranteedImpulseTarget);
    }

    private void TriggerExplosionAudioVfx(Vector3 originWorld)
    {
        if (shouldPlayExplosionAudio && !string.IsNullOrEmpty(explosionAudioEventName))
            AudioManager.Play(explosionAudioEventName);

        var pos = originWorld + transform.TransformVector(explosionVfxLocalOffset);

        EffectManager.TryGetVFX(explosionVFX, new VFXEmitterParams()
        {
            position = pos,
            rotation = Quaternion.identity,
            scale = explosionRadiusMeters
        }, out var _);
    }

    private void ApplyPlayerLaunch(Rigidbody playerRigidbody, float targetSpeed)
    {
        Vector3 launchDirectionWorld = transform.up;
        Vector3 v = playerRigidbody.velocity;
        float along = Vector3.Dot(v, launchDirectionWorld);
        Vector3 tangential = v - launchDirectionWorld * along;
        playerRigidbody.velocity = tangential + launchDirectionWorld * targetSpeed;
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
                return;
            }

            controller.OnNotify(EntityController.DESTROY);
            return;
        }
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
                    cachedModel.TryAddComponent(HealthComponentFactory);
                }
            }
        }
        else
        {
            if (ensureRoutine == null) ensureRoutine = StartCoroutine(EnsureHealthRegisteredDeferred());
        }
    }

    private HealthComponent HealthComponentFactory()
    {
        return new HealthComponent(healthComponentData);
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

    private bool IsMedalConditionActive()
    {
        return GlobalLevelManager.BarrelInvulnerability;
    }

    private bool IsPlayerObject(Transform t)
    {
        if (!t) return false;
        if (t.GetComponentInParent<PlayerController>() != null) return true;
        var root = t.root;
        return root && root.CompareTag(requiredPlayerTag);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Vector3 origin = transform.TransformPoint(explosionOriginLocalOffset);
        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.85f);
        Gizmos.DrawWireSphere(origin, Mathf.Max(0f, explosionRadiusMeters));

        Gizmos.color = IsMedalConditionActive() ? Color.cyan : Color.yellow;
        Gizmos.DrawRay(origin, (transform.up).normalized * Mathf.Min(1.0f, explosionRadiusMeters * 0.5f));
    }
}
