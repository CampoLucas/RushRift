using Game;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class LockOnBlinkView : MonoBehaviour
{
    public enum DisplayMode { AutoShowHide, AlwaysVisible }

    [Header("Ability Reference")]
    [SerializeField, Tooltip("LockOnBlink ability instance attached to the player. If empty, it will auto-bind on Start.")]
    private LockOnBlink lockOnBlinkAbility;

    [Header("Progress (Radial)")]
    [SerializeField, Tooltip("Filled radial Image that visualizes lock progress (0..1).")]
    private Image lockProgressImage;

    [SerializeField, Tooltip("Display behavior for the progress image.")]
    private DisplayMode progressDisplayMode = DisplayMode.AutoShowHide;

    [SerializeField, Tooltip("If true, uses unscaled time for UI timing.")]
    private bool useUnscaledTimeForUi = true;

    [SerializeField, Tooltip("Keep the progress visible briefly after cancel (AutoShowHide mode).")]
    private float uiVisibilityGraceSeconds = 0.15f;

    [SerializeField, Tooltip("Radial fill origin for the progress image.")]
    private Image.Origin360 radialFillOrigin = Image.Origin360.Top;

    [SerializeField, Tooltip("If true, sets fill direction clockwise.")]
    private bool radialFillClockwise = true;

    [Header("Crosshair Sprites")]
    [SerializeField, Tooltip("Image component used for the crosshair.")]
    private Image crosshairImage;

    [SerializeField, Tooltip("Sprite used when no lockable target is being aimed at.")]
    private Sprite crosshairNormalSprite;

    [SerializeField, Tooltip("Sprite used while aiming a valid lockable target.")]
    private Sprite crosshairLockSprite;

    [Header("Audio")]
    [SerializeField, Tooltip("If enabled, plays a sound once when the aim first touches a valid lockable target.")]
    private bool playTargetLockedSfx = true;

    [SerializeField, Tooltip("Audio event name played when the aim first touches a lockable target.")]
    private string targetLockedSfxEventName = "TargetLocked";

    [SerializeField, Tooltip("Minimum time between consecutive TargetLocked SFX plays.")]
    private float targetLockedRetriggerCooldownSeconds = 0.15f;

    [Header("Auto Setup")]
    [SerializeField, Tooltip("If true and no progress image is assigned, fetches the first Image in children (inactive included).")]
    private bool autoFindChildImageIfMissing = true;

    [Header("Debug")]
    [SerializeField, Tooltip("Enable debug logs for the view.")]
    private bool isDebugLoggingEnabled = false;

    private bool isProgressCurrentlyVisible;
    private float hideAtAbsoluteTime;

    private bool lastHasLockableTarget;
    private float nextTargetLockedAllowedTime;

    private const string PlayerTag = "Player";
    private float Now => useUnscaledTimeForUi ? Time.unscaledTime : Time.time;

    private void Awake()
    {
        if (!lockProgressImage && autoFindChildImageIfMissing)
            lockProgressImage = GetComponentInChildren<Image>(true);

        if (lockProgressImage)
        {
            lockProgressImage.type = Image.Type.Filled;
            lockProgressImage.fillMethod = Image.FillMethod.Radial360;
            lockProgressImage.fillOrigin = (int)radialFillOrigin;
            lockProgressImage.fillClockwise = radialFillClockwise;
            lockProgressImage.fillAmount = 0f;
        }

        ApplyInitialVisibility();
        hideAtAbsoluteTime = 0f;
        SetCrosshairLocked(null);

        lastHasLockableTarget = false;
        nextTargetLockedAllowedTime = 0f;
    }

    private void OnEnable()
    {
        EnsureSubscribed();
        ApplyInitialVisibility();
        hideAtAbsoluteTime = 0f;
        RefreshCrosshairImmediate();

        lastHasLockableTarget = false;
        nextTargetLockedAllowedTime = 0f;
    }

    private void Start()
    {
        if (!lockOnBlinkAbility) AutoBindFromPlayerTag();
        EnsureSubscribed();
        ApplyInitialVisibility();
        RefreshCrosshairImmediate();
    }

    private void OnDisable()
    {
        Unsubscribe();
        if (lockProgressImage) lockProgressImage.gameObject.SetActive(false);
        isProgressCurrentlyVisible = false;
        hideAtAbsoluteTime = 0f;

        SetCrosshairLocked(null);
        lastHasLockableTarget = false;
    }

    private void Update()
    {
        if (progressDisplayMode == DisplayMode.AutoShowHide && isProgressCurrentlyVisible && hideAtAbsoluteTime > 0f && Now >= hideAtAbsoluteTime)
            SetProgressVisible(false);

        bool canSwapCrosshair = lockOnBlinkAbility && lockOnBlinkAbility.IsAbilityAvailable();

        Transform target = null;
        if (canSwapCrosshair)
            target = lockOnBlinkAbility.ProbeAimedLockableTarget();

        bool hasTarget = target;

        if (!canSwapCrosshair)
        {
            lastHasLockableTarget = false;
            SetCrosshairLocked(null);
            return;
        }

        if (playTargetLockedSfx && hasTarget && !lastHasLockableTarget && Now >= nextTargetLockedAllowedTime && !string.IsNullOrEmpty(targetLockedSfxEventName))
        {
            AudioManager.Play(targetLockedSfxEventName);
            nextTargetLockedAllowedTime = Now + Mathf.Max(0f, targetLockedRetriggerCooldownSeconds);
            Log("TargetLocked SFX played");
        }

        lastHasLockableTarget = hasTarget;
        SetCrosshairLocked(target);
    }

    private void ApplyInitialVisibility()
    {
        if (!lockProgressImage) return;
        if (progressDisplayMode == DisplayMode.AlwaysVisible) SetProgressVisible(true);
        else SetProgressVisible(false);
    }

    private void HandleLockStarted(Transform target)
    {
        if (lockProgressImage)
        {
            lockProgressImage.fillAmount = 0f;
            hideAtAbsoluteTime = 0f;
            if (progressDisplayMode == DisplayMode.AutoShowHide) SetProgressVisible(true);
        }
        Log(target ? $"Lock started on {target.name}" : "Lock started");
    }

    private void HandleLockProgress(float progress01)
    {
        if (!lockProgressImage) return;
        lockProgressImage.fillAmount = Mathf.Clamp01(progress01);
        hideAtAbsoluteTime = 0f;
        if (progressDisplayMode == DisplayMode.AutoShowHide && !isProgressCurrentlyVisible) SetProgressVisible(true);
    }

    private void HandleLockReady()
    {
        if (!lockProgressImage) return;
        lockProgressImage.fillAmount = 1f;
        hideAtAbsoluteTime = 0f;
        Log("Lock ready");
    }

    private void HandleLockCanceled()
    {
        if (!lockProgressImage) return;
        lockProgressImage.fillAmount = 0f;
        if (progressDisplayMode == DisplayMode.AutoShowHide)
            hideAtAbsoluteTime = Now + Mathf.Max(0f, uiVisibilityGraceSeconds);
        Log("Lock canceled");
    }

    private void HandleBlinkExecuted(Vector3 destination)
    {
        hideAtAbsoluteTime = 0f;
        Log($"Blink executed to {destination}");
    }

    private void RefreshCrosshairImmediate()
    {
        Transform t = lockOnBlinkAbility && lockOnBlinkAbility.IsAbilityAvailable()
            ? lockOnBlinkAbility.ProbeAimedLockableTarget()
            : null;
        SetCrosshairLocked(t);
    }

    private void SetCrosshairLocked(Transform candidate)
    {
        if (!crosshairImage) return;
        bool locked = candidate;
        Sprite desired = locked && crosshairLockSprite ? crosshairLockSprite : crosshairNormalSprite;
        if (desired && crosshairImage.sprite != desired) crosshairImage.sprite = desired;
    }

    private void SetProgressVisible(bool visible)
    {
        if (!lockProgressImage) return;
        lockProgressImage.gameObject.SetActive(visible);
        isProgressCurrentlyVisible = visible;
    }

    private void EnsureSubscribed()
    {
        if (!lockOnBlinkAbility) return;
        lockOnBlinkAbility.OnLockStarted += HandleLockStarted;
        lockOnBlinkAbility.OnLockProgressChanged += HandleLockProgress;
        lockOnBlinkAbility.OnLockReady += HandleLockReady;
        lockOnBlinkAbility.OnLockCanceled += HandleLockCanceled;
        lockOnBlinkAbility.OnBlinkExecuted += HandleBlinkExecuted;
    }

    private void Unsubscribe()
    {
        if (!lockOnBlinkAbility) return;
        lockOnBlinkAbility.OnLockStarted -= HandleLockStarted;
        lockOnBlinkAbility.OnLockProgressChanged -= HandleLockProgress;
        lockOnBlinkAbility.OnLockReady -= HandleLockReady;
        lockOnBlinkAbility.OnLockCanceled -= HandleLockCanceled;
        lockOnBlinkAbility.OnBlinkExecuted -= HandleBlinkExecuted;
    }

    private void AutoBindFromPlayerTag()
    {
        var player = GameObject.FindGameObjectWithTag(PlayerTag);
        if (!player) return;

        var ability = player.GetComponentInChildren<LockOnBlink>(true);
        if (!ability) ability = player.GetComponent<LockOnBlink>();
        if (!ability) return;

        Unsubscribe();
        lockOnBlinkAbility = ability;
        EnsureSubscribed();
        RefreshCrosshairImmediate();
    }

    private void Log(string msg)
    {
        if (!isDebugLoggingEnabled) return;
        Debug.Log($"[LockOnBlinkView] {name}: {msg}", this);
    }
}