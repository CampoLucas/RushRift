using Game;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class LockOnBlinkView : MonoBehaviour
{
    public enum DisplayMode { AutoShowHide, AlwaysVisible }

    [Header("Ability Reference")]
    [SerializeField, Tooltip("LockOnBlink ability instance attached to the player.")]
    private LockOnBlink lockOnBlinkAbility;

    [Header("UI")]
    [SerializeField, Tooltip("Slider visualizing lock progress (0..1).")]
    private Slider lockProgressSlider;

    [SerializeField, Tooltip("Display behavior for the slider.")]
    private DisplayMode sliderDisplayMode = DisplayMode.AutoShowHide;

    [SerializeField, Tooltip("Keep the slider visible briefly after release/cancel to prevent flicker (AutoShowHide mode).")]
    private float uiVisibilityGraceSeconds = 0.15f;

    [SerializeField, Tooltip("If true, uses unscaled time for UI timing.")]
    private bool useUnscaledTimeForUi = true;

    [Header("Crosshair Sprites")]
    [SerializeField, Tooltip("Image component used for the crosshair.")]
    private Image crosshairImage;

    [SerializeField, Tooltip("Sprite used when no lockable target is being aimed at.")]
    private Sprite crosshairNormalSprite;

    [SerializeField, Tooltip("Sprite used while aiming a valid lockable target.")]
    private Sprite crosshairLockSprite;

    [Header("Crosshair World Offset")]
    [SerializeField, Tooltip("If enabled, offsets the crosshair toward the target's on-screen position.")]
    private bool enableCrosshairWorldOffset = true;

    [SerializeField, Tooltip("Canvas containing the crosshair.")]
    private Canvas targetCanvas;

    [SerializeField, Tooltip("Optional camera for Screen Space - Camera canvases; leave empty for Screen Space - Overlay or to auto-pick Camera.main.")]
    private Camera canvasCameraOverride;

    [SerializeField, Tooltip("Maximum offset distance in pixels from the base crosshair position.")]
    private float crosshairOffsetMaxPixels = 48f;

    [SerializeField, Tooltip("Interpolation speed toward the desired offset.")]
    private float crosshairOffsetLerpSpeed = 14f;

    [SerializeField, Tooltip("If true, crosshair offsets only when a lockable target is detected; otherwise offsets toward any probed target if available.")]
    private bool onlyOffsetWhenLockable = true;

    [SerializeField, Tooltip("Extra vertical offset in pixels applied to the lock crosshair when a target is present (positive is up).")]
    private float crosshairAdditionalYOffsetPixels = 0f;

    [Header("Audio")]
    [SerializeField, Tooltip("If enabled, plays a sound once when the aim first touches a valid lockable target.")]
    private bool playTargetLockedSfx = true;

    [SerializeField, Tooltip("Audio event name played when the aim first touches a lockable target.")]
    private string targetLockedSfxEventName = "TargetLocked";

    [SerializeField, Tooltip("Minimum time between consecutive TargetLocked SFX plays to avoid jitter spam.")]
    private float targetLockedRetriggerCooldownSeconds = 0.15f;

    [Header("Auto Setup")]
    [SerializeField, Tooltip("If true and no slider is assigned, fetches the first Slider in children (inactive included).")]
    private bool autoFindChildSliderIfMissing = true;

    [Header("Debug")]
    [SerializeField, Tooltip("Enable debug logs for the view.")]
    private bool isDebugLoggingEnabled = false;

    private bool isSliderCurrentlyVisible;
    private float hideAtAbsoluteTime;

    private RectTransform crosshairRect;
    private RectTransform canvasRect;
    private Vector2 crosshairBaseAnchoredPosition;
    private Vector2 crosshairCurrentAnchoredPosition;
    private Camera resolvedCanvasCamera;

    private bool lastHasLockableTarget;
    private float nextTargetLockedAllowedTime;

    private float Now => useUnscaledTimeForUi ? Time.unscaledTime : Time.time;
    private float Dt => useUnscaledTimeForUi ? Time.unscaledDeltaTime : Time.deltaTime;

    private void Awake()
    {
        if (!lockProgressSlider && autoFindChildSliderIfMissing)
            lockProgressSlider = GetComponentInChildren<Slider>(true);

        if (lockProgressSlider)
        {
            lockProgressSlider.minValue = 0f;
            lockProgressSlider.maxValue = 1f;
            lockProgressSlider.value = 0f;
        }

        crosshairRect = crosshairImage ? crosshairImage.rectTransform : null;
        if (!targetCanvas && crosshairRect) targetCanvas = crosshairRect.GetComponentInParent<Canvas>();
        canvasRect = targetCanvas ? targetCanvas.GetComponent<RectTransform>() : null;
        resolvedCanvasCamera = canvasCameraOverride ? canvasCameraOverride : Camera.main;

        if (crosshairRect)
        {
            crosshairBaseAnchoredPosition = crosshairRect.anchoredPosition;
            crosshairCurrentAnchoredPosition = crosshairBaseAnchoredPosition;
        }

        ApplyInitialVisibility();
        hideAtAbsoluteTime = 0f;
        SetCrosshairLocked(null);

        lastHasLockableTarget = false;
        nextTargetLockedAllowedTime = 0f;
    }

    private void OnEnable()
    {
        if (lockOnBlinkAbility)
        {
            lockOnBlinkAbility.OnLockStarted += HandleLockStarted;
            lockOnBlinkAbility.OnLockProgressChanged += HandleLockProgress;
            lockOnBlinkAbility.OnLockReady += HandleLockReady;
            lockOnBlinkAbility.OnLockCanceled += HandleLockCanceled;
            lockOnBlinkAbility.OnBlinkExecuted += HandleBlinkExecuted;
        }

        ApplyInitialVisibility();
        hideAtAbsoluteTime = 0f;
        RefreshCrosshairImmediate();
        ResetCrosshairOffsetImmediate();

        lastHasLockableTarget = false;
        nextTargetLockedAllowedTime = 0f;
    }

    private void Start()
    {
        ApplyInitialVisibility();
        RefreshCrosshairImmediate();
        ResetCrosshairOffsetImmediate();
    }

    private void OnDisable()
    {
        if (lockOnBlinkAbility)
        {
            lockOnBlinkAbility.OnLockStarted -= HandleLockStarted;
            lockOnBlinkAbility.OnLockProgressChanged -= HandleLockProgress;
            lockOnBlinkAbility.OnLockReady -= HandleLockReady;
            lockOnBlinkAbility.OnLockCanceled -= HandleLockCanceled;
            lockOnBlinkAbility.OnBlinkExecuted -= HandleBlinkExecuted;
        }

        if (lockProgressSlider) lockProgressSlider.gameObject.SetActive(false);
        isSliderCurrentlyVisible = false;
        hideAtAbsoluteTime = 0f;

        SetCrosshairLocked(null);
        ResetCrosshairOffsetImmediate();

        lastHasLockableTarget = false;
    }

    private void Update()
    {
        if (sliderDisplayMode == DisplayMode.AutoShowHide && isSliderCurrentlyVisible && hideAtAbsoluteTime > 0f && Now >= hideAtAbsoluteTime)
            SetSliderVisible(false);

        Transform target = lockOnBlinkAbility ? lockOnBlinkAbility.ProbeAimedLockableTarget() : null;
        bool hasTarget = target;

        if (playTargetLockedSfx && hasTarget && !lastHasLockableTarget && Now >= nextTargetLockedAllowedTime && !string.IsNullOrEmpty(targetLockedSfxEventName))
        {
            AudioManager.Play(targetLockedSfxEventName);
            nextTargetLockedAllowedTime = Now + Mathf.Max(0f, targetLockedRetriggerCooldownSeconds);
            Log("TargetLocked SFX played");
        }

        lastHasLockableTarget = hasTarget;

        SetCrosshairLocked(target);

        if (enableCrosshairWorldOffset)
            TickCrosshairOffset(target);
    }

    private void ApplyInitialVisibility()
    {
        if (!lockProgressSlider) return;
        if (sliderDisplayMode == DisplayMode.AlwaysVisible) SetSliderVisible(true);
        else SetSliderVisible(false);
    }

    private void HandleLockStarted(Transform target)
    {
        if (lockProgressSlider)
        {
            lockProgressSlider.value = 0f;
            hideAtAbsoluteTime = 0f;
            if (sliderDisplayMode == DisplayMode.AutoShowHide) SetSliderVisible(true);
        }
        Log(target ? $"Lock started on {target.name}" : "Lock started");
    }

    private void HandleLockProgress(float progress01)
    {
        if (!lockProgressSlider) return;
        lockProgressSlider.value = progress01;
        hideAtAbsoluteTime = 0f;
        if (sliderDisplayMode == DisplayMode.AutoShowHide && !isSliderCurrentlyVisible) SetSliderVisible(true);
    }

    private void HandleLockReady()
    {
        if (!lockProgressSlider) return;
        lockProgressSlider.value = 1f;
        hideAtAbsoluteTime = 0f;
        Log("Lock ready");
    }

    private void HandleLockCanceled()
    {
        if (!lockProgressSlider) return;
        lockProgressSlider.value = 0f;
        if (sliderDisplayMode == DisplayMode.AutoShowHide)
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
        Transform t = lockOnBlinkAbility ? lockOnBlinkAbility.ProbeAimedLockableTarget() : null;
        SetCrosshairLocked(t);
    }

    private void SetCrosshairLocked(Transform candidate)
    {
        if (!crosshairImage) return;
        bool locked = candidate;
        Sprite desired = locked && crosshairLockSprite ? crosshairLockSprite : crosshairNormalSprite;
        if (desired && crosshairImage.sprite != desired) crosshairImage.sprite = desired;
    }

    private void ResetCrosshairOffsetImmediate()
    {
        if (!crosshairRect) return;
        crosshairCurrentAnchoredPosition = crosshairBaseAnchoredPosition;
        crosshairRect.anchoredPosition = crosshairCurrentAnchoredPosition;
    }

    private void TickCrosshairOffset(Transform target)
    {
        if (!crosshairRect || !canvasRect || !targetCanvas) return;

        bool shouldOffset = target || !onlyOffsetWhenLockable;
        Vector2 desired = crosshairBaseAnchoredPosition;

        if (shouldOffset)
        {
            Vector2 targetLocal;
            bool ok = target ? TryWorldToCanvasLocal(target.position, out targetLocal) : TryWorldToCanvasLocal(Vector3.positiveInfinity, out targetLocal);
            if (ok)
            {
                targetLocal.y += crosshairAdditionalYOffsetPixels;
                Vector2 from = crosshairBaseAnchoredPosition;
                Vector2 dir = targetLocal - from;
                float mag = dir.magnitude;
                if (mag > 0.0001f) dir /= mag;
                float clamped = Mathf.Min(mag, Mathf.Max(0f, crosshairOffsetMaxPixels));
                desired = from + dir * clamped;
            }
        }

        crosshairCurrentAnchoredPosition = Vector2.Lerp(crosshairCurrentAnchoredPosition, desired, 1f - Mathf.Exp(-crosshairOffsetLerpSpeed * Dt));
        crosshairRect.anchoredPosition = crosshairCurrentAnchoredPosition;
    }

    private bool TryWorldToCanvasLocal(Vector3 worldPos, out Vector2 localPoint)
    {
        localPoint = crosshairBaseAnchoredPosition;
        if (!targetCanvas) return false;

        var renderMode = targetCanvas.renderMode;
        if (renderMode == RenderMode.ScreenSpaceOverlay)
        {
            if (!resolvedCanvasCamera) resolvedCanvasCamera = Camera.main;
            Vector3 sp = resolvedCanvasCamera ? resolvedCanvasCamera.WorldToScreenPoint(worldPos) : new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, sp, null, out localPoint);
        }
        else
        {
            var cam = canvasCameraOverride ? canvasCameraOverride : resolvedCanvasCamera;
            if (!cam) cam = Camera.main;
            resolvedCanvasCamera = cam;
            if (!cam) return false;
            Vector3 sp = cam.WorldToScreenPoint(worldPos);
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, sp, cam, out localPoint);
        }
    }

    private void SetSliderVisible(bool visible)
    {
        if (!lockProgressSlider) return;
        lockProgressSlider.gameObject.SetActive(visible);
        isSliderCurrentlyVisible = visible;
    }

    private void Log(string msg)
    {
        if (!isDebugLoggingEnabled) return;
        Debug.Log($"[LockOnBlinkView] {name}: {msg}", this);
    }
}