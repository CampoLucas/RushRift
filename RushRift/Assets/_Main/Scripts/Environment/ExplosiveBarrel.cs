using System.Collections;
using System.Collections.Generic;
using Game;
using Game.Entities;
using Game.Entities.Components;
using UnityEngine;
using UnityEngine.VFX;
using _Main.Scripts.Feedbacks;
using Game.VFX;
using MyTools.Global;
using UnityEngine.Serialization;

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
    [SerializeField] private HealthComponentData healthComponentData;
    [SerializeField, Tooltip("If enabled, maps HealthComponentData.OnZeroHealth to the post explosion action.")]
    private bool useDieBehaviourForPostAction = true;

    [Header("Explosion Settings")]
    [FormerlySerializedAs("shouldExplodeOnDestroy")] [SerializeField] private bool explodeOnDestroy = true;
    [FormerlySerializedAs("explosionOriginLocalOffset")] [SerializeField] private Vector3 explosionOffset = Vector3.zero;
    [FormerlySerializedAs("explosionRadiusMeters")] [SerializeField] private float explosionRadius = 6f;
    [FormerlySerializedAs("explosionDelaySeconds")] [SerializeField] private float explosionDelay = 0f;

    [Header("Distance-Based Damage")]
    [FormerlySerializedAs("explosionDamageMax")] [SerializeField] private float maxDamage = 100f;
    [FormerlySerializedAs("explosionDamageMin")] [SerializeField] private float minDamage = 10f;

    [Header("Distance-Based Impulse")]
    [FormerlySerializedAs("playerLaunchSpeedMaxMetersPerSecond")] [SerializeField] private float maxImpulseSpeed = 18f;
    [FormerlySerializedAs("playerLaunchSpeedMinMetersPerSecond")] [SerializeField] private float minImpulseSpeed = 6f;
    [FormerlySerializedAs("otherRigidbodiesExplosionImpulseMax")] [SerializeField] private float otherMaxImpulse = 20f;
    [FormerlySerializedAs("otherRigidbodiesExplosionImpulseMin")] [SerializeField] private float otherMinImpulse = 4f;
    [FormerlySerializedAs("otherRigidbodiesUpwardsModifier")] [SerializeField] private float otherUpwardsModifier = 0.5f;

    [Header("Post Explosion Action")]
    [SerializeField] private PostExplosionAction postExplosionAction = PostExplosionAction.DestroyGameObject;
    [FormerlySerializedAs("postExplosionActionDelaySeconds")] [SerializeField] private float postDelay = 0f;

    [Header("Player Target")]
    [FormerlySerializedAs("requiredPlayerTag")] [SerializeField] private string targetTag = "Player";
    [SerializeField] private LaunchDirectionSource launchDirectionSource = LaunchDirectionSource.RadialFromBarrelCenter;
    [FormerlySerializedAs("customLaunchDirection")] [SerializeField] private Vector3 customDirection = Vector3.up;
    [FormerlySerializedAs("directionReferenceTransform")] [SerializeField] private Transform directionReference;
    [FormerlySerializedAs("shouldResetTangentialVelocity")] [SerializeField, Tooltip("If true, zeroes sideways velocity relative to the launch direction; otherwise preserves it.")]
    private bool resetTangentialVel;

    [Header("Enemy Targeting")]
    [SerializeField] private string enemyTag = "Enemy";

    [Header("Audio")]
    [FormerlySerializedAs("explosionAudioEventName")] [SerializeField] private string explosionAudioName = "BarrelExplosion";

    [Header("Visual Effects")]
    [SerializeField] private VFXPrefabID explosionVFX;
    [FormerlySerializedAs("explosionVfxLocalOffset")] [SerializeField] private Vector3 explosionVfxOffset = Vector3.zero;

    private static Collider[] _overlapBuffer = new Collider[128];
    private bool _explosionTriggered;
    private bool _explosionInitiatedByOnDestroy;

    private NullCheck<EntityController> _cachedController;
    private NullCheck<HealthComponent> _healthComponent;
    private Coroutine _ensureRoutine;

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
        SyncPostActionFromDieBehaviour();
        if (explosionDelay > 0f && _explosionTriggered) Invoke(nameof(ExecuteExplosion), explosionDelay);
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
        if (_ensureRoutine != null)
        {
            StopCoroutine(_ensureRoutine);
            _ensureRoutine = null;
        }
    }

    private void Update()
    {
        if (!_explosionTriggered && _healthComponent.TryGetValue(out var component) && !component.IsAlive())
        {
            _explosionTriggered = true;
            ExecuteExplosion();
        }
    }

    #region Execute Methods

    public void TriggerExplosion(Vector3? overrideWorldOrigin = null, bool useMaxForce = false, NullCheck<Rigidbody> target = new())
    {
        if (_explosionTriggered) return;
        _explosionTriggered = true;
        _explosionInitiatedByOnDestroy = false;
        var origin = overrideWorldOrigin ?? transform.TransformPoint(explosionOffset);
        ExecuteExplosion(origin, useMaxForce, target);
    }
    
    
    private void ExecuteExplosion()
    {
        var origin = transform.TransformPoint(explosionOffset);
        ExecuteExplosion(origin, false, null);
    }
    
    private void ExecuteExplosion(Vector3 origin, bool maxForce, NullCheck<Rigidbody> target)
    {
        TriggerExplosionAudioVfxAndFreeze(origin);

        var count = Physics.OverlapSphereNonAlloc(origin, Mathf.Max(0f, explosionRadius), _overlapBuffer, ~0, QueryTriggerInteraction.Collide);
        var byGroup = new Dictionary<Transform, AggregatedHit>(count);

        for (var i = 0; i < count; i++)
        {
            var col = _overlapBuffer[i];
            if (!col) continue;

            var controller = col.GetComponentInParent<EntityController>();
            var group = controller ? controller.Origin.transform : (col.attachedRigidbody ? col.attachedRigidbody.transform : col.transform);

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

            var closest = origin;
            var d = Vector3.Distance(origin, closest);
            if (d < agg.MinDistance)
            {
                agg.MinDistance = d;
                agg.HasDistance = true;
            }
        }

        if (target.TryGetValue(out var body))
        {
            var group = body.transform;
            if (!byGroup.TryGetValue(group, out var agg))
            {
                agg = new AggregatedHit { Group = group, MinDistance = 0f, HasDistance = true, Rigidbody = target };
                byGroup.Add(group, agg);
            }
            else
            {
                agg.Rigidbody = target;
                agg.MinDistance = 0f;
                agg.HasDistance = true;
            }
        }

        foreach (var kv in byGroup)
        {
            var agg = kv.Value;
            var distance = agg.HasDistance ? agg.MinDistance : Vector3.Distance(origin, agg.Group.position);
            var closeness = Mathf.Clamp01(1f - Mathf.InverseLerp(0f, Mathf.Max(0.0001f, explosionRadius), distance));

            var scaledDamage = Mathf.Lerp(minDamage, maxDamage, closeness);

            var impulseT = maxForce ? 1f : closeness;
            var scaledPlayerSpeed = Mathf.Lerp(minImpulseSpeed, maxImpulseSpeed, impulseT);
            var scaledOtherImpulse = Mathf.Lerp(otherMinImpulse, otherMaxImpulse, impulseT);

            var targetObject = agg.Controller ? agg.Controller.Origin.gameObject : agg.Group.gameObject;
            var isPlayer = !string.IsNullOrEmpty(targetTag) && targetObject.CompareTag(targetTag);
            var isEnemyByTag = !string.IsNullOrEmpty(enemyTag) && targetObject.CompareTag(enemyTag);
            var isEnemyByType = agg.Controller is EnemyController;
            var isEnemy = isEnemyByTag || isEnemyByType;

            if (isPlayer)
            {
                if (agg.Rigidbody)
                    ApplyJumpPadStyleLaunch(agg.Rigidbody, origin, true, Mathf.Max(0f, scaledPlayerSpeed));

                if (!IsMedalConditionActive())
                    TryDealScaledDamageOrNotify(targetObject, "Player", origin, Mathf.Max(0f, scaledDamage));
            }
            else
            {
                TryDealScaledDamageOrNotify(targetObject, isEnemy ? "Enemy" : "Entity", origin, Mathf.Max(0f, scaledDamage));

                if (agg.Rigidbody && scaledOtherImpulse > 0f)
                    agg.Rigidbody.AddExplosionForce(scaledOtherImpulse, origin, explosionRadius, otherUpwardsModifier, ForceMode.Impulse);
            }

            this.Log($"Applied | name={targetObject.name} dist={distance:0.##} close={closeness:0.##} dmg={scaledDamage:0.##} impulse={scaledOtherImpulse:0.##}");
        }

        if (!_explosionInitiatedByOnDestroy)
        {
            if (postDelay > 0f)
            {
                StartCoroutine(DelayedPostExplosionAction(postDelay));
            }
            else
            {
                DoPostExplosionAction();
            }
        }

        this.Log($"Explosion done | origin={origin} radius={explosionRadius} medalActive={IsMedalConditionActive()}");
    }

    #endregion
    
    // Should be in a View class executed bia events or observers
    private void TriggerExplosionAudioVfxAndFreeze(Vector3 originWorld)
    {
        if (!string.IsNullOrEmpty(explosionAudioName))
            AudioManager.Play(explosionAudioName);
        
        var pos = originWorld + transform.TransformVector(explosionVfxOffset);

        EffectManager.TryGetVFX(explosionVFX, new VFXEmitterParams()
        {
            position = pos,
            rotation = Quaternion.identity,
            scale = explosionRadius
        }, out var emitter);
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

    private void ApplyJumpPadStyleLaunch(Rigidbody body, Vector3 origin, bool isPlayer, float targetSpeed)
    {
        var launchDirectionWorld = ComputeLaunchDirection(body, origin, isPlayer);
        var v = body.velocity;
        var along = Vector3.Dot(v, launchDirectionWorld);
        var tangential = v - launchDirectionWorld * along;
        if (resetTangentialVel) tangential = Vector3.zero;
        body.velocity = tangential + launchDirectionWorld * Mathf.Max(0f, targetSpeed);
    }

    private Vector3 ComputeLaunchDirection(Rigidbody body, Vector3 origin, bool isPlayer)
    {
#if true
        if (isPlayer && IsMedalConditionActive()) return Vector3.up;

        var dir = launchDirectionSource switch
        {
            LaunchDirectionSource.BarrelUp => transform.up,
            LaunchDirectionSource.BarrelForward => transform.forward,
            LaunchDirectionSource.RadialFromBarrelCenter => body.position - origin,
            LaunchDirectionSource.CustomVector => customDirection,
            LaunchDirectionSource.ReferenceTransformForward => directionReference
                ? directionReference.forward
                : transform.forward,
            LaunchDirectionSource.ReferenceTransformUp => directionReference ? directionReference.up : transform.up,
            _ => Vector3.up
        };

        if (dir.sqrMagnitude < 1e-6f) dir = Vector3.up;
        return dir.normalized;
#else
        var dir = targetRigidbody.position - explosionOriginWorld;

        return dir.normalized;
#endif
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
        _cachedController = GetComponentInParent<EntityController>();
        if (!_cachedController) _cachedController = GetComponent<EntityController>();
    }

    private void EnsureHealthRegisteredImmediateOrDeferred()
    {
        if (_healthComponent) return;

        if (_cachedController.TryGetValue(out var controller))
        {
            var model = controller.GetModel();
            if (model.TryGetComponent<HealthComponent>(out var component))
            {
                _healthComponent = component;
            }
            else if (healthComponentData != null)
            {
                _healthComponent = new HealthComponent(healthComponentData);
                model.TryAddComponent(_healthComponent.Get());
            }
            
            SyncPostActionFromDieBehaviour();
        }
        else
        {
            _ensureRoutine ??= StartCoroutine(EnsureHealthRegisteredDeferred());
        }
    }

    private IEnumerator EnsureHealthRegisteredDeferred()
    {
        int safetyFrames = 16;
        while (safetyFrames-- > 0 && _healthComponent)
        {
            ResolveControllerAndModel();
            
            if (_cachedController)
            {
                EnsureHealthRegisteredImmediateOrDeferred();
                if (_healthComponent) break;
            }
            yield return null;
        }
        _ensureRoutine = null;
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
        return LevelManager.BarrelInvulnerabilityEnabled;
    }
    
    private void OnDestroy()
    {
        StopAllCoroutines();

        if (Application.isPlaying)
        {
            if (explodeOnDestroy && !_explosionTriggered)
            {
                _explosionInitiatedByOnDestroy = true;
                _explosionTriggered = true;
                ExecuteExplosion();
            }
        }

        healthComponentData = null;
        directionReference = null;
        _overlapBuffer = null;
        _cachedController = null;
        _healthComponent = null;
        _ensureRoutine = null;
    }

    private void OnDrawGizmosSelected()
    {
        var origin = transform.TransformPoint(explosionOffset);
        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.85f);
        Gizmos.DrawWireSphere(origin, Mathf.Max(0f, explosionRadius));

        var exampleDirection = Vector3.up;
        switch (launchDirectionSource)
        {
            case LaunchDirectionSource.BarrelUp: exampleDirection = transform.up; break;
            case LaunchDirectionSource.BarrelForward: exampleDirection = transform.forward; break;
            case LaunchDirectionSource.RadialFromBarrelCenter: exampleDirection = Vector3.up; break;
            case LaunchDirectionSource.CustomVector: exampleDirection = (customDirection.sqrMagnitude < 1e-6f) ? Vector3.up : customDirection.normalized; break;
            case LaunchDirectionSource.ReferenceTransformForward: exampleDirection = directionReference ? directionReference.forward : transform.forward; break;
            case LaunchDirectionSource.ReferenceTransformUp: exampleDirection = directionReference ? directionReference.up : transform.up; break;
        }

        Gizmos.color = IsMedalConditionActive() ? Color.cyan : Color.yellow;
        Gizmos.DrawRay(origin, exampleDirection.normalized * Mathf.Min(1.0f, explosionRadius * 0.5f));
    }
}