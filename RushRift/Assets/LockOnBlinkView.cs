using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class LockOnBlinkView : MonoBehaviour
{
    public enum DisplayMode
    {
        AutoShowHide,
        AlwaysVisible
    }

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

    [Header("Auto Setup")]
    [SerializeField, Tooltip("If true and no slider is assigned, fetches the first Slider in children (inactive included).")]
    private bool autoFindChildSliderIfMissing = true;

    [Header("Debug")]
    [SerializeField, Tooltip("Enable debug logs for the view.")]
    private bool isDebugLoggingEnabled = false;

    private bool isSliderCurrentlyVisible;
    private float hideAtAbsoluteTime;

    private float Now => useUnscaledTimeForUi ? Time.unscaledTime : Time.time;

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

        ApplyInitialVisibility();
        hideAtAbsoluteTime = 0f;
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
    }

    private void Start()
    {
        ApplyInitialVisibility();
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
    }

    private void Update()
    {
        if (sliderDisplayMode != DisplayMode.AutoShowHide) return;
        if (!isSliderCurrentlyVisible) return;
        if (hideAtAbsoluteTime <= 0f) return;
        if (Now >= hideAtAbsoluteTime) SetSliderVisible(false);
    }

    private void ApplyInitialVisibility()
    {
        if (!lockProgressSlider) return;

        if (sliderDisplayMode == DisplayMode.AlwaysVisible)
        {
            SetSliderVisible(true);
            return;
        }

        SetSliderVisible(false);
    }

    private void HandleLockStarted(Transform target)
    {
        if (!lockProgressSlider) return;
        lockProgressSlider.value = 0f;
        hideAtAbsoluteTime = 0f;
        if (sliderDisplayMode == DisplayMode.AutoShowHide) SetSliderVisible(true);
        Log($"Lock started on {target.name}");
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
        if (!lockProgressSlider) return;
        hideAtAbsoluteTime = 0f;
        Log($"Blink executed to {destination}");
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