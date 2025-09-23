using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class LockOnBlink : MonoBehaviour
{
    public enum OffsetSpace { World, TargetLocal }
    public enum TimeMode { Scaled, Unscaled }
    public enum LockStartMode { Automatic, OnKeyHold, OnKeyPress }

    [Header("Lock Start Mode")]
    [SerializeField, Tooltip("Automatic: lock charges when a target is in sight. OnKeyHold: hold Lock Key to charge; release to blink. OnKeyPress: toggle charge on key press.")]
    private LockStartMode lockStartMode = LockStartMode.OnKeyHold;

    [Header("Keys")]
    [SerializeField, Tooltip("Lock key. In OnKeyHold, holding this charges and releasing it attempts the blink (single key flow).")]
    private KeyCode lockKey = KeyCode.F;
    [SerializeField, Tooltip("Blink key used in Automatic / OnKeyPress modes. Ignored in OnKeyHold.")]
    private KeyCode blinkKey = KeyCode.F;

    [Header("Cooldown & Time")]
    [SerializeField, Tooltip("Cooldown between blinks in seconds.")]
    private float cooldownSeconds = 1f;
    [SerializeField, Tooltip("Time system for general timing (except optional unscaled during slow-mo lock below).")]
    private TimeMode timeMode = TimeMode.Scaled;

    [Header("Slow Motion While Locking")]
    [SerializeField, Tooltip("If enabled, time slows while the lock is charging.")]
    private bool slowTimeWhileCharging = true;
    [SerializeField, Tooltip("Time.timeScale while charging (e.g. 0.15–0.3).")]
    private float slowTimeScale = 0.2f;
    [SerializeField, Tooltip("Also scale FixedDeltaTime while slowed to keep physics stable.")]
    private bool adjustFixedDeltaWhileSlowed = true;
    [SerializeField, Tooltip("Use Unscaled time for the lock timer while slowed so the progress bar fills consistently.")]
    private bool useUnscaledForLockTimerWhenSlowed = true;

    [Header("Targeting")]
    [SerializeField, Tooltip("Camera used to aim the lock. If empty, main camera is used.")]
    private Transform aimCameraTransform;
    [SerializeField, Tooltip("Maximum distance to acquire a target.")]
    private float maxLockDistance = 60f;
    [SerializeField, Tooltip("Radius of the spherecast used to acquire a target from the center of the screen.")]
    private float lockSphereRadius = 0.35f;
    [SerializeField, Tooltip("Targets must be on these layers.")]
    private LayerMask targetLayers = ~0;
    [SerializeField, Tooltip("Optional tag filter. Leave empty to ignore tags.")]
    private string requiredTargetTag = "Enemy";
    [SerializeField, Tooltip("Require a clear line of sight to the target.")]
    private bool requireLineOfSight = true;
    [SerializeField, Tooltip("Max hits buffer for the non-alloc spherecast.")]
    private int spherecastMaxHits = 64;

    [Header("Lock & Blink")]
    [SerializeField, Tooltip("Time you must keep an enemy in sight to arm the blink.")]
    private float lockOnTimeSeconds = 0.3f;
    [SerializeField, Tooltip("Offset applied to the destination position when blinking.")]
    private Vector3 destinationOffset = new Vector3(0f, 0f, -1.25f);
    [SerializeField, Tooltip("Space in which the destination offset is expressed.")]
    private OffsetSpace destinationOffsetSpace = OffsetSpace.TargetLocal;
    [SerializeField, Tooltip("Snap player rotation to face the target after teleport.")]
    private bool snapRotationToTarget = true;
    [SerializeField, Tooltip("If enabled, resets velocity of a Rigidbody (if present) after teleport.")]
    private bool zeroOutRigidbodyVelocity = true;

    [Header("UI")]
    [SerializeField, Tooltip("Optional UI slider to visualize lock progress (0..1).")]
    private Slider lockOnProgressSlider;
    [SerializeField, Tooltip("If true, automatically show the slider during lock and hide it otherwise.")]
    private bool autoShowHideSlider = true;

    [Header("State & References")]
    [SerializeField, Tooltip("Optional explicit reference to a Rigidbody on the player.")]
    private Rigidbody playerRigidbody;

    [Header("Debug")]
    [SerializeField, Tooltip("Enable debug logging.")]
    private bool isDebugLoggingEnabled;
    [SerializeField, Tooltip("Draw gizmos for targeting and last blink destination.")]
    private bool drawGizmos = true;
    [SerializeField, Tooltip("Color used to draw gizmos.")]
    private Color gizmoColor = new Color(0.2f, 0.9f, 1f, 0.9f);

    private Transform _currentTarget;
    private Transform _chargingTarget;
    private Vector3 _lastBlinkDestination;
    private float _cooldownUntil;
    private float _lockTimer;
    private bool _readyToBlink;
    private bool _chargingActive;

    private RaycastHit[] _hitsBuffer;
    private Camera _aimCam;

    private bool _slowMoActive;

    private static int s_slowMoOwners;
    private static float s_originalTimeScale = 1f;
    private static float s_originalFixedDelta = 0.02f;
    private static bool s_originalCaptured;

    private float Now => timeMode == TimeMode.Unscaled ? Time.unscaledTime : Time.time;

    private float Dt
    {
        get
        {
            if (slowTimeWhileCharging && _slowMoActive && useUnscaledForLockTimerWhenSlowed)
                return Time.unscaledDeltaTime;
            return timeMode == TimeMode.Unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
        }
    }

    private void Awake()
    {
        if (!aimCameraTransform)
        {
            var cam = Camera.main;
            if (cam) aimCameraTransform = cam.transform;
        }
        _aimCam = aimCameraTransform ? aimCameraTransform.GetComponent<Camera>() : null;

        if (!playerRigidbody) playerRigidbody = GetComponent<Rigidbody>();

        spherecastMaxHits = Mathf.Max(8, spherecastMaxHits);
        _hitsBuffer = new RaycastHit[spherecastMaxHits];

        if (lockOnProgressSlider)
        {
            lockOnProgressSlider.minValue = 0f;
            lockOnProgressSlider.maxValue = 1f;
            lockOnProgressSlider.value = 0f;
            if (autoShowHideSlider) lockOnProgressSlider.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        ResetLockState(true);
        ReleaseSlowMoIfOwned();
    }

    private void Update()
    {
        HandleChargingInput();
        TickLocking();

        if (lockStartMode == LockStartMode.OnKeyHold)
        {
            if (lockKey != KeyCode.None && Input.GetKeyUp(lockKey))
            {
                if (_readyToBlink) { TryPerformBlink(); }
                else { ResetLockState(true); ReleaseSlowMoIfOwned(); }
            }
        }
        else
        {
            if (blinkKey != KeyCode.None && Input.GetKeyDown(blinkKey))
                TryPerformBlink();
        }
    }

    private void HandleChargingInput()
    {
        switch (lockStartMode)
        {
            case LockStartMode.Automatic:
                _chargingActive = true;
                break;

            case LockStartMode.OnKeyHold:
            {
                if (lockKey == KeyCode.None) { _chargingActive = false; return; }
                bool isDown = Input.GetKey(lockKey);
                bool isUpThisFrame = Input.GetKeyUp(lockKey);
                _chargingActive = isDown || isUpThisFrame; // keep active on release frame so blink can trigger
                break;
            }

            case LockStartMode.OnKeyPress:
                if (lockKey != KeyCode.None && Input.GetKeyDown(lockKey))
                    _chargingActive = !_chargingActive;
                if (!_chargingActive) { ResetLockState(); ReleaseSlowMoIfOwned(); }
                break;
        }
    }

    private void TickLocking()
    {
        if (Now < _cooldownUntil)
        {
            ResetLockState();
            ReleaseSlowMoIfOwned();
            return;
        }

        if (!_chargingActive)
        {
            if (lockStartMode != LockStartMode.OnKeyHold)
            {
                ResetLockState();
                ReleaseSlowMoIfOwned();
            }
            return;
        }

        var target = AcquireTarget();
        _currentTarget = target;

        if (!target)
        {
            ResetLockState();
            ReleaseSlowMoIfOwned();
            return;
        }

        if (slowTimeWhileCharging && !_slowMoActive) AcquireSlowMo();

        if (_chargingTarget != target)
        {
            _chargingTarget = target;
            _lockTimer = 0f;
            _readyToBlink = false;
            if (lockOnProgressSlider)
            {
                lockOnProgressSlider.value = 0f;
                if (autoShowHideSlider) lockOnProgressSlider.gameObject.SetActive(true);
            }
            Log($"Lock started → {_chargingTarget.name}");
        }

        if (!_readyToBlink)
        {
            _lockTimer += Dt;
            float t = Mathf.Clamp01(_lockTimer / Mathf.Max(0.0001f, lockOnTimeSeconds));
            if (lockOnProgressSlider) lockOnProgressSlider.value = t;

            if (_lockTimer >= lockOnTimeSeconds)
            {
                _readyToBlink = true;
                if (lockOnProgressSlider) lockOnProgressSlider.value = 1f;
                Log("Lock ready");
            }
        }
    }

    private void TryPerformBlink()
    {
        if (!_readyToBlink) { Log("Blink ignored: lock not ready"); return; }
        if (!_currentTarget) { Log("Blink ignored: no target"); ResetLockState(true); ReleaseSlowMoIfOwned(); return; }
        if (Now < _cooldownUntil) { Log("Blink ignored: on cooldown"); return; }

        PerformBlink(_currentTarget);
        _cooldownUntil = Now + Mathf.Max(0f, cooldownSeconds);

        if (lockStartMode != LockStartMode.Automatic)
            _chargingActive = false;

        ResetLockState(true);
        ReleaseSlowMoIfOwned();
    }

    private void PerformBlink(Transform target)
    {
        Vector3 targetPos = target.position;
        Vector3 offset = destinationOffsetSpace == OffsetSpace.TargetLocal
            ? target.TransformVector(destinationOffset)
            : destinationOffset;

        Vector3 destination = targetPos + offset;
        _lastBlinkDestination = destination;

        transform.position = destination;

        if (snapRotationToTarget)
        {
            Vector3 dir = (target.position - transform.position);
            if (dir.sqrMagnitude > 1e-6f)
            {
                Vector3 flat = new Vector3(dir.x, 0f, dir.z);
                var look = flat.sqrMagnitude > 1e-6f ? flat : dir;
                transform.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
            }
        }

        if (playerRigidbody && zeroOutRigidbodyVelocity)
            playerRigidbody.velocity = Vector3.zero;

        Log($"Blink → {destination}");
    }

    private void ResetLockState(bool forceHide = false)
    {
        _chargingTarget = null;
        _lockTimer = 0f;
        _readyToBlink = false;

        if (lockOnProgressSlider)
        {
            lockOnProgressSlider.value = 0f;
            if (autoShowHideSlider && (forceHide || _currentTarget == null || !_chargingActive))
                lockOnProgressSlider.gameObject.SetActive(false);
        }
    }

    private Transform AcquireTarget()
    {
        if (!_aimCam) return null;

        var ray = _aimCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        int hitCount = Physics.SphereCastNonAlloc(ray, lockSphereRadius, _hitsBuffer, maxLockDistance, targetLayers, QueryTriggerInteraction.Ignore);

        Transform best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            ref var h = ref _hitsBuffer[i];
            var tr = h.collider.transform;

            if (!string.IsNullOrEmpty(requiredTargetTag) && !tr.CompareTag(requiredTargetTag))
                continue;

            if (requireLineOfSight)
            {
                Vector3 dir = (h.point - _aimCam.transform.position).normalized;
                if (Physics.Raycast(_aimCam.transform.position, dir, out var block, h.distance - 0.01f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (block.collider.transform != tr && !IsChildOf(block.collider.transform, tr))
                        continue;
                }
            }

            if (h.distance < bestDist) { bestDist = h.distance; best = tr; }
        }

        return best;
    }

    private void AcquireSlowMo()
    {
        if (_slowMoActive) return;
        if (!s_originalCaptured)
        {
            s_originalCaptured = true;
            s_originalTimeScale = Time.timeScale;
            s_originalFixedDelta = Time.fixedDeltaTime;
        }

        s_slowMoOwners++;
        _slowMoActive = true;

        Time.timeScale = Mathf.Clamp(slowTimeScale, 0.01f, 1f);
        if (adjustFixedDeltaWhileSlowed)
            Time.fixedDeltaTime = s_originalFixedDelta * Time.timeScale;

        Log($"SlowMo ON (owners={s_slowMoOwners}, scale={Time.timeScale:0.###})");
    }

    private void ReleaseSlowMoIfOwned()
    {
        if (!_slowMoActive) return;
        _slowMoActive = false;
        s_slowMoOwners = Mathf.Max(0, s_slowMoOwners - 1);

        if (s_slowMoOwners == 0)
        {
            Time.timeScale = s_originalTimeScale;
            if (adjustFixedDeltaWhileSlowed)
                Time.fixedDeltaTime = s_originalFixedDelta;
            Log("SlowMo OFF (restored original scales)");
        }
        else
        {
            Log($"SlowMo owner released (owners remaining={s_slowMoOwners})");
        }
    }

    private static bool IsChildOf(Transform child, Transform potentialParent)
    {
        var t = child;
        while (t != null)
        {
            if (t == potentialParent) return true;
            t = t.parent;
        }
        return false;
    }

    private void Log(string msg)
    {
        if (!isDebugLoggingEnabled) return;
        Debug.Log($"[LockOnBlink] {name}: {msg}", this);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        Gizmos.color = gizmoColor;

        if (aimCameraTransform)
        {
            var cam = aimCameraTransform.GetComponent<Camera>();
            if (cam)
            {
                var ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
                Gizmos.DrawRay(ray.origin, ray.direction * Mathf.Min(8f, maxLockDistance));
            }
        }

        if (_currentTarget)
        {
            Gizmos.DrawWireSphere(_currentTarget.position, 0.25f);
            Gizmos.DrawLine(transform.position, _currentTarget.position);
        }

        if (_lastBlinkDestination != default)
            Gizmos.DrawWireCube(_lastBlinkDestination, Vector3.one * 0.2f);
    }
#endif
}