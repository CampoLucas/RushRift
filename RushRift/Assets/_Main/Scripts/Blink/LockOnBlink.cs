using _Main.Scripts.Feedbacks;
using UnityEngine;
using Game;
using Game.Entities;
using Game.Entities.Components;

[DisallowMultipleComponent]
public class LockOnBlink : MonoBehaviour
{
    public enum OffsetSpace { World, TargetLocal }
    public enum TimeMode { Scaled, Unscaled }
    public enum LockStartMode { Automatic, OnKeyHold, OnKeyPress }

    [Header("Upgrade Gate")]
    [SerializeField, Tooltip("If true, uses Local Enabled instead of LevelManager.CanUseLockOnBlink.")]
    private bool overrideUpgradeGate;

    [SerializeField, Tooltip("Local enable used only when Override Gate is true.")]
    private bool localUpgradeEnabled;
    
    [SerializeField, Tooltip("Read-only: reflects whether the ability is currently usable, considering medal/override gate.")]
    private bool abilityGateMirror;

    [Header("Lock Start Mode")]
    [SerializeField, Tooltip("Automatic: lock charges when a target is in sight. OnKeyHold: hold Lock Key to charge; release to blink. OnKeyPress: toggle charge on key press.")]
    private LockStartMode lockStartMode = LockStartMode.OnKeyHold;

    [Header("Keys")]
    [SerializeField, Tooltip("Lock key. In OnKeyHold, holding this charges and releasing it attempts the blink.")]
    private KeyCode lockKey = KeyCode.F;
    [SerializeField, Tooltip("Blink key used in Automatic / OnKeyPress modes. Ignored in OnKeyHold.")]
    private KeyCode blinkKey = KeyCode.F;

    [Header("Cooldown & Time")]
    [SerializeField, Tooltip("Cooldown between blinks in seconds.")]
    private float cooldownSeconds = 1f;
    [SerializeField, Tooltip("Time system for general timing.")]
    private TimeMode timeMode = TimeMode.Scaled;

    [Header("Slow Motion While Locking")]
    [SerializeField, Tooltip("If enabled, time slows while the lock is charging.")]
    private bool slowTimeWhileCharging = true;
    [SerializeField, Tooltip("Time.timeScale while charging.")]
    private float slowTimeScale = 0.2f;
    [SerializeField, Tooltip("Also scale FixedDeltaTime while slowed.")]
    private bool adjustFixedDeltaWhileSlowed = true;
    [SerializeField, Tooltip("Use Unscaled delta time for the lock timer while slowed.")]
    private bool useUnscaledForLockTimerWhenSlowed = true;

    [Header("Targeting")]
    [SerializeField, Tooltip("Camera used to aim the lock. If empty, main camera is used.")]
    private Transform aimCameraTransform;
    [SerializeField, Tooltip("Maximum distance to acquire a target.")]
    private float maxLockDistance = 60f;
    [SerializeField, Tooltip("Base radius of the spherecast used to acquire a target from the center of the screen.")]
    private float lockSphereRadius = 0.35f;
    [SerializeField, Tooltip("Radius to reach while charging the lock (ramped from base to this value).")]
    private float lockSphereRadiusWhileCharging = 0.6f;
    [SerializeField, Tooltip("How the lock sphere radius ramps from base to the charging value over the lock time.")]
    private AnimationCurve lockRadiusRamp = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField, Tooltip("Targets must be on these layers.")]
    private LayerMask targetLayers = ~0;
    [SerializeField, Tooltip("Optional tag filter. Leave empty to ignore tags.")]
    private string requiredTargetTag = "Enemy";
    [SerializeField, Tooltip("Require a clear line of sight to the target.")]
    private bool requireLineOfSight = true;
    [SerializeField, Tooltip("Max hits buffer for the non-alloc spherecast.")]
    private int spherecastMaxHits = 64;
    
    [Header("Crosshair Probe")]
    [SerializeField, Tooltip("If true, crosshair probes use the base lock sphere radius instead of the charging radius.")]
    private bool crosshairProbeUsesBaseRadius = true;

    [Header("Target Stickiness")]
    [SerializeField, Tooltip("If enabled, the ability will keep the current target briefly through minor aim jitter or brief LOS loss.")]
    private bool enableTargetStickiness = true;
    [SerializeField, Tooltip("Seconds to retain the current target after it momentarily drops out.")]
    private float targetRetainGraceSeconds = 0.25f;
    [SerializeField, Tooltip("Max angle change (degrees) allowed before switching to a different target.")]
    private float retargetAngleToleranceDegrees = 10f;
    [SerializeField, Tooltip("Max change in target distance (meters) allowed before switching to a different target.")]
    private float retargetDistanceToleranceMeters = 1.0f;

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

    [Header("On-Blink Kill")]
    [SerializeField, Tooltip("If enabled, the target you blink to will be destroyed.")]
    private bool shouldKillTargetOnBlink = true;
    [SerializeField, Tooltip("If true, tries to kill via HealthComponent.Intakill; otherwise falls back to EntityController.DESTROY.")]
    private bool preferHealthComponentKill = true;

    [Header("Audio")]
    [SerializeField, Tooltip("If enabled, plays an SFX when locking begins.")]
    private bool shouldPlayLockChargingSfx = true;
    [SerializeField, Tooltip("Audio event name played when lock starts.")]
    private string lockChargingSfxEventName = "Blink";

    [Header("Auto Blink")]
    [SerializeField, Tooltip("If enabled, automatically teleports as soon as the lock completes.")]
    private bool shouldAutoBlinkWhenReady;

    [Header("Lock Visual FX")]
    [SerializeField, Tooltip("If enabled, applies chromatic aberration and vignette while locking, and resets on release.")]
    private bool enableLockVisualFx = true;
    [SerializeField, Tooltip("Target chromatic aberration intensity while locking.")]
    private float lockFxChromaticTarget = 1f;
    [SerializeField, Tooltip("Seconds to reach chromatic target.")]
    private float lockFxChromaticInDurationSeconds = 0.35f;
    [SerializeField, Tooltip("Seconds to return chromatic to 0 on release.")]
    private float lockFxChromaticOutDurationSeconds = 0.25f;
    [SerializeField, Tooltip("Target vignette intensity while locking.")]
    private float lockFxVignetteTarget = 0.6f;
    [SerializeField, Tooltip("Seconds to reach vignette target.")]
    private float lockFxVignetteInDurationSeconds = 0.25f;
    [SerializeField, Tooltip("Seconds to return vignette to 0 on release.")]
    private float lockFxVignetteOutDurationSeconds = 0.25f;
    [SerializeField, Tooltip("If true, the FX tweens use unscaled time.")]
    private bool lockFxUseUnscaledTime = true;

    [Header("Lock Music Low-Pass")]
    [SerializeField, Tooltip("If enabled, applies a music low-pass while locking and clears it when lock is released.")]
    private bool enableLockMusicLowPass = true;
    [SerializeField, Tooltip("Paused-state cutoff in Hz while locking.")]
    private float lockLowPassPausedCutoffHz = 250f;
    [SerializeField, Tooltip("Seconds to ramp into paused cutoff.")]
    private float lockLowPassPauseRampSeconds = 0.05f;
    [SerializeField, Tooltip("Seconds to ramp back to unpaused after release.")]
    private float lockLowPassResumeRampSeconds = 0.25f;

    [Header("State & References")]
    [SerializeField, Tooltip("Optional explicit reference to a Rigidbody on the player.")]
    private Rigidbody playerRigidbody;

    [Header("Debug & Gizmos")]
    [SerializeField, Tooltip("Enable debug logging.")]
    private bool isDebugLoggingEnabled;
    [SerializeField, Tooltip("Draw gizmos for targeting and last blink destination.")]
    private bool drawGizmos = true;
    [SerializeField, Tooltip("Color used to draw gizmos.")]
    private Color gizmoColor = new Color(0.2f, 0.9f, 1f, 0.9f);

    public System.Action<Transform> OnLockStarted;
    public System.Action<float> OnLockProgressChanged;
    public System.Action OnLockReady;
    public System.Action OnLockCanceled;
    public System.Action<Vector3> OnBlinkExecuted;

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

    private float _lastSeenTargetAtTime;
    private float _lastTargetDistance;
    private Vector3 _lastTargetDirFromCam;

    private bool _lockFxActive;

    private float Now => timeMode == TimeMode.Unscaled ? Time.unscaledTime : Time.time;

    private float Dt
    {
        get
        {
            if (slowTimeWhileCharging && _slowMoActive && useUnscaledForLockTimerWhenSlowed) return Time.unscaledDeltaTime;
            return timeMode == TimeMode.Unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
        }
    }

    private bool IsAbilityEnabled()
    {
        return overrideUpgradeGate ? localUpgradeEnabled : Game.LevelManager.CanUseLockOnBlink;
    }

    public Transform GetCurrentTarget() => _currentTarget;
    public float GetCooldownRemaining() => Mathf.Max(0f, _cooldownUntil - Now);
    public bool IsReadyToBlink() => _readyToBlink;
    
    public bool IsAbilityAvailable()
    {
        bool open = IsAbilityEnabled();
        abilityGateMirror = open;
        return open;
    }

    private void Awake()
    {
        if (!aimCameraTransform) { var cam = Camera.main; if (cam) aimCameraTransform = cam.transform; }
        _aimCam = aimCameraTransform ? aimCameraTransform.GetComponent<Camera>() : null;
        if (!playerRigidbody) playerRigidbody = GetComponent<Rigidbody>();
        spherecastMaxHits = Mathf.Max(8, spherecastMaxHits);
        _hitsBuffer = new RaycastHit[spherecastMaxHits];
    }

    private void OnDisable()
    {
        ResetLockState(true);
        ReleaseSlowMoIfOwned();
    }

    private void Update()
    {
        if (!IsAbilityEnabled())
        {
            ResetLockState(true);
            ReleaseSlowMoIfOwned();
            return;
        }

        HandleChargingInput();
        TickLocking();

        if (lockStartMode == LockStartMode.OnKeyHold)
        {
            if (lockKey != KeyCode.None && Input.GetKeyUp(lockKey))
            {
                if (_readyToBlink) { TryPerformBlink(); }
                else { StopLockAudioNow(); ResetLockState(true); ReleaseSlowMoIfOwned(); }
            }
        }
        else
        {
            if (blinkKey != KeyCode.None && Input.GetKeyDown(blinkKey)) TryPerformBlink();
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
                _chargingActive = isDown || isUpThisFrame;
                break;
            }
            case LockStartMode.OnKeyPress:
                if (lockKey != KeyCode.None && Input.GetKeyDown(lockKey)) _chargingActive = !_chargingActive;
                if (!_chargingActive) { ResetLockState(); ReleaseSlowMoIfOwned(); }
                break;
        }
    }

    private void TickLocking()
    {
        if (Now < _cooldownUntil)
        {
            StopLockAudioNow();
            ResetLockState();
            ReleaseSlowMoIfOwned();
            return;
        }

        if (!_chargingActive)
        {
            if (lockStartMode != LockStartMode.OnKeyHold)
            {
                StopLockAudioNow();
                ResetLockState();
                ReleaseSlowMoIfOwned();
            }
            return;
        }

        var raw = AcquireTargetRaw();
        var canonical = CanonicalizeTarget(raw);

        if (enableTargetStickiness)
        {
            canonical = ApplyStickiness(canonical);
        }

        _currentTarget = canonical;

        if (!_currentTarget)
        {
            StopLockAudioNow();
            ResetLockState();
            ReleaseSlowMoIfOwned();
            return;
        }

        if (slowTimeWhileCharging && !_slowMoActive) AcquireSlowMo();

        if (_chargingTarget != _currentTarget)
        {
            _chargingTarget = _currentTarget;
            _lockTimer = 0f;
            _readyToBlink = false;
            OnLockStarted?.Invoke(_chargingTarget);
            OnLockProgressChanged?.Invoke(0f);
            if (shouldPlayLockChargingSfx && !string.IsNullOrEmpty(lockChargingSfxEventName)) AudioManager.Play(lockChargingSfxEventName);
            if (enableLockVisualFx) StartLockVisualFx();
            if (enableLockMusicLowPass) StartLockMusicLowPass();
            Log($"Lock started â†’ {_chargingTarget.name}");
        }

        if (!_readyToBlink)
        {
            _lockTimer += Dt;
            float t = Mathf.Clamp01(_lockTimer / Mathf.Max(0.0001f, lockOnTimeSeconds));
            OnLockProgressChanged?.Invoke(t);

            if (_lockTimer >= lockOnTimeSeconds)
            {
                _readyToBlink = true;
                OnLockProgressChanged?.Invoke(1f);
                OnLockReady?.Invoke();
                Log("Lock ready");
                if (shouldAutoBlinkWhenReady) TryPerformBlink();
            }
        }
    }

    public Transform ProbeAimedLockableTarget()
    {
        if (!_aimCam) return null;
        float radius = crosshairProbeUsesBaseRadius
            ? Mathf.Max(0f, lockSphereRadius)
            : LockOnBlinkUtilities.ComputeDynamicLockRadius(_chargingActive, lockOnTimeSeconds, _lockTimer, lockSphereRadius, lockSphereRadiusWhileCharging, lockRadiusRamp);

        var raw = LockOnBlinkUtilities.AcquireTargetRaw(_aimCam, radius, maxLockDistance, targetLayers, requiredTargetTag, requireLineOfSight, _hitsBuffer);
        return LockOnBlinkUtilities.CanonicalizeTarget(raw);
    }
    
    private Transform AcquireTargetRaw()
    {
        return LockOnBlinkUtilities.AcquireTargetRaw(_aimCam, GetDynamicLockRadius(), maxLockDistance, targetLayers, requiredTargetTag, requireLineOfSight, _hitsBuffer);
    }

    private Transform CanonicalizeTarget(Transform tr)
    {
        return LockOnBlinkUtilities.CanonicalizeTarget(tr);
    }

    private Transform ApplyStickiness(Transform candidate)
    {
        return LockOnBlinkUtilities.ApplyStickiness(_chargingTarget, candidate, _aimCam, Now, ref _lastSeenTargetAtTime, ref _lastTargetDirFromCam, ref _lastTargetDistance, retargetAngleToleranceDegrees, retargetDistanceToleranceMeters, targetRetainGraceSeconds);
    }

    private void TryPerformBlink()
    {
        if (!_readyToBlink) { Log("Blink ignored: lock not ready"); return; }
        if (!_currentTarget) { Log("Blink ignored: no target"); ResetLockState(true); ReleaseSlowMoIfOwned(); return; }
        if (Now < _cooldownUntil) { Log("Blink ignored: on cooldown"); return; }

        PerformBlink(_currentTarget);
        if (shouldKillTargetOnBlink) TryKillTarget(_currentTarget);

        _cooldownUntil = Now + Mathf.Max(0f, cooldownSeconds);

        if (lockStartMode != LockStartMode.Automatic) _chargingActive = false;

        ResetLockState(true);
        ReleaseSlowMoIfOwned();
    }

    private void PerformBlink(Transform target)
    {
        Vector3 offset = destinationOffsetSpace == OffsetSpace.TargetLocal ? target.TransformVector(destinationOffset) : destinationOffset;
        Vector3 destination = target.position + offset;
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

        if (playerRigidbody && zeroOutRigidbodyVelocity) playerRigidbody.velocity = Vector3.zero;

        OnBlinkExecuted?.Invoke(destination);
        Log($"Blink â†’ {destination}");
    }

    private void TryKillTarget(Transform target)
    {
        if (!target) return;

        var controller = target.GetComponentInParent<EntityController>();
        var barrel = target.GetComponentInParent<ExplosiveBarrel>();
        if (barrel) barrel.TriggerExplosion(null, true, playerRigidbody);

        if (controller != null)
        {
            var model = controller.GetModel();
            if (preferHealthComponentKill && model != null && model.TryGetComponent<HealthComponent>(out var health))
            {
                health.Intakill(target.position);
                Log($"Killed via HealthComponent.Instakill | target={controller.Origin.name}");
                return;
            }

            controller.OnNotify(EntityController.DESTROY);
            Log($"Destroyed via EntityController observer | target={controller.Origin.name}");
        }
    }

    private float GetDynamicLockRadius()
    {
        return LockOnBlinkUtilities.ComputeDynamicLockRadius(_chargingActive, lockOnTimeSeconds, _lockTimer, lockSphereRadius, lockSphereRadiusWhileCharging, lockRadiusRamp);
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
        if (adjustFixedDeltaWhileSlowed) Time.fixedDeltaTime = s_originalFixedDelta * Time.timeScale;
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
            if (adjustFixedDeltaWhileSlowed) Time.fixedDeltaTime = s_originalFixedDelta;
            Log("SlowMo OFF (restored original scales)");
        }
        else
        {
            Log($"SlowMo owner released (owners remaining={s_slowMoOwners})");
        }
    }

    private void ResetLockState(bool hardReset = false)
    {
        bool wasCharging = _chargingTarget != null || _currentTarget != null || _readyToBlink || _lockTimer > 0f;
        _lockTimer = 0f;
        _readyToBlink = false;
        if (hardReset)
        {
            _chargingTarget = null;
            _currentTarget = null;
        }
        if (wasCharging) OnLockCanceled?.Invoke();
        if (enableLockVisualFx) StopLockVisualFx();
        StopLockMusicLowPass();
        StopLockAudioNow();
    }

    private void StartLockVisualFx()
    {
        if (_lockFxActive) return;
        _lockFxActive = true;
        LockOnBlinkUtilities.StartLockVisualFx(enableLockVisualFx, lockFxChromaticTarget, lockFxChromaticInDurationSeconds, lockFxVignetteTarget, lockFxVignetteInDurationSeconds, lockFxUseUnscaledTime);
    }

    private void StopLockVisualFx()
    {
        if (!_lockFxActive) return;
        _lockFxActive = false;
        LockOnBlinkUtilities.StopLockVisualFx(enableLockVisualFx, lockFxChromaticTarget, lockFxChromaticOutDurationSeconds, lockFxVignetteTarget, lockFxVignetteOutDurationSeconds, lockFxUseUnscaledTime);
    }

    private void StartLockMusicLowPass()
    {
        if (!enableLockMusicLowPass) return;
        MusicLowPassService.SetPaused(true, Mathf.Max(10f, lockLowPassPausedCutoffHz), Mathf.Max(0f, lockLowPassPauseRampSeconds), Mathf.Max(0f, lockLowPassResumeRampSeconds));
    }

    private void StopLockMusicLowPass()
    {
        if (!enableLockMusicLowPass) return;
        MusicLowPassService.SetPaused(false, Mathf.Max(10f, lockLowPassPausedCutoffHz), Mathf.Max(0f, lockLowPassPauseRampSeconds), Mathf.Max(0f, lockLowPassResumeRampSeconds));
    }

    private void StopLockAudioNow()
    {
        if (shouldPlayLockChargingSfx && !string.IsNullOrEmpty(lockChargingSfxEventName))
            AudioManager.Stop(lockChargingSfxEventName);
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
        if (_lastBlinkDestination != default) Gizmos.DrawWireCube(_lastBlinkDestination, Vector3.one * 0.2f);
    }
#endif
}