#if UNITY_EDITOR && BLINK_DEBUG_ENABLED
#define BLINK_DEBUG
#endif

using Game;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Add the BLINK_DEBUG_ENABLED to allow debugs on the editor
/// </summary>
[DisallowMultipleComponent]
public class LockOnBlinkView : MonoBehaviour
{
    public enum DisplayMode { AutoShowHide, AlwaysVisible }

    [Header("Ability Reference")]
    [SerializeField] private LockOnBlink lockOnBlinkAbility;

    [Header("Progress (Radial)")]
    [SerializeField] private Image lockProgressImage;
    [SerializeField] private DisplayMode progressDisplayMode = DisplayMode.AutoShowHide;
    [SerializeField] private bool useUnscaledTimeForUi = true;

    [Tooltip("Keep the progress visible briefly after cancel (AutoShowHide mode).")]
    [SerializeField] private float uiVisibilityGraceSeconds = 0.15f;

    [SerializeField] private Image.Origin360 radialFillOrigin = Image.Origin360.Top;
    [SerializeField] private bool radialFillClockwise = true;

    [Header("Crosshair Sprites")]
    [SerializeField] private Image crosshairImage;
    [SerializeField] private Sprite crosshairNormalSprite;
    [SerializeField] private Sprite crosshairLockSprite;

    [Header("Audio")]
    [Tooltip("If enabled, plays a sound once when the aim first touches a valid lockable target.")]
    [SerializeField] private bool playTargetLockedSfx = true;

    [Tooltip("Audio event name played when the aim first touches a lockable target.")]
    [SerializeField] private string targetLockedSfxEventName = "TargetLocked";

    [Tooltip("Minimum time between consecutive TargetLocked SFX plays.")]
    [SerializeField] private float targetLockedRetriggerCooldownSeconds = 0.15f;

    [Header("Auto Setup")]
    [SerializeField] private bool autoFindChildImageIfMissing = true;

    private bool _isProgressCurrentlyVisible;
    private float _hideAtAbsoluteTime;

    private bool _lastHasLockableTarget;
    private float _nextTargetLockedAllowedTime;

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
        _hideAtAbsoluteTime = 0f;
        SetCrosshairLocked(null);

        _lastHasLockableTarget = false;
        _nextTargetLockedAllowedTime = 0f;
    }

    private void OnEnable()
    {
        EnsureSubscribed();
        ApplyInitialVisibility();
        _hideAtAbsoluteTime = 0f;
        RefreshCrosshairImmediate();

        _lastHasLockableTarget = false;
        _nextTargetLockedAllowedTime = 0f;
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
        _isProgressCurrentlyVisible = false;
        _hideAtAbsoluteTime = 0f;

        SetCrosshairLocked(null);
        _lastHasLockableTarget = false;
    }

    private void Update()
    {
        if (progressDisplayMode == DisplayMode.AutoShowHide && _isProgressCurrentlyVisible && _hideAtAbsoluteTime > 0f && Now >= _hideAtAbsoluteTime)
            SetProgressVisible(false);

        bool canSwapCrosshair = lockOnBlinkAbility && lockOnBlinkAbility.IsAbilityAvailable();

        Transform target = null;
        if (canSwapCrosshair)
            target = lockOnBlinkAbility.ProbeAimedLockableTarget();

        bool hasTarget = target;

        if (!canSwapCrosshair)
        {
            _lastHasLockableTarget = false;
            SetCrosshairLocked(null);
            return;
        }

        if (playTargetLockedSfx && hasTarget && !_lastHasLockableTarget && Now >= _nextTargetLockedAllowedTime && !string.IsNullOrEmpty(targetLockedSfxEventName))
        {
            AudioManager.Play(targetLockedSfxEventName);
            _nextTargetLockedAllowedTime = Now + Mathf.Max(0f, targetLockedRetriggerCooldownSeconds);
            Log("TargetLocked SFX played");
        }

        _lastHasLockableTarget = hasTarget;
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
            _hideAtAbsoluteTime = 0f;
            if (progressDisplayMode == DisplayMode.AutoShowHide) SetProgressVisible(true);
        }
        Log(target ? $"Lock started on {target.name}" : "Lock started");
    }

    private void HandleLockProgress(float progress01)
    {
        if (!lockProgressImage) return;
        lockProgressImage.fillAmount = Mathf.Clamp01(progress01);
        _hideAtAbsoluteTime = 0f;
        if (progressDisplayMode == DisplayMode.AutoShowHide && !_isProgressCurrentlyVisible) SetProgressVisible(true);
    }

    private void HandleLockReady()
    {
        if (!lockProgressImage) return;
        lockProgressImage.fillAmount = 1f;
        _hideAtAbsoluteTime = 0f;
        Log("Lock ready");
    }

    private void HandleLockCanceled()
    {
        if (!lockProgressImage) return;
        lockProgressImage.fillAmount = 0f;
        if (progressDisplayMode == DisplayMode.AutoShowHide)
            _hideAtAbsoluteTime = Now + Mathf.Max(0f, uiVisibilityGraceSeconds);
        Log("Lock canceled");
    }

    private void HandleBlinkExecuted(Vector3 destination)
    {
        _hideAtAbsoluteTime = 0f;
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
        _isProgressCurrentlyVisible = visible;
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

    [System.Diagnostics.Conditional("BLINK_DEBUG")]
    private void Log(string msg)
    {
        Debug.Log($"[LockOnBlinkView] {name}: {msg}", this);
    }
}