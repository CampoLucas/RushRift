using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Triggers a screen flash using an Image, supporting color overrides.
/// </summary>
[AddComponentMenu("UI/Effects/Screen Flash")]
[RequireComponent(typeof(Image))]
public class ScreenFlash : MonoBehaviour
{
    #region Serialized Fields

    [Header("Flash Settings")]
    [Tooltip("Default color of the flash.")]
    [SerializeField] private Color flashColor = Color.white;

    [Tooltip("Curve controlling fade in/out.")]
    [SerializeField] private AnimationCurve flashCurve = AnimationCurve.EaseInOut(0, 0, 1, 0);

    #endregion

    #region Private Fields

    private Image _image;
    private Coroutine _flashCoroutine;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        _image = GetComponent<Image>();
        _image.enabled = true;
        _image.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
        _image.raycastTarget = false;

        if (flashCurve == null || flashCurve.length == 0)
        {
            flashCurve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(1f, 0f)
            );
        }

        if (!Instance) Instance = this;
    }

    #endregion

    #region Public API

    public static ScreenFlash Instance { get; private set; }
    
    public void TriggerFlash(string hexColor, float alpha, float duration)
    {
        if (ColorUtility.TryParseHtmlString(hexColor, out Color color))
        {
            TriggerFlash(color, alpha, duration);
        }
        else
        {
            Debug.LogWarning($"Invalid hex color: {hexColor}");
        }
    }


    public void TriggerFlash(Color color, float alpha, float duration)
    {
        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
        }

        _flashCoroutine = StartCoroutine(FlashCoroutine(color, alpha, duration));
    }

    
    #endregion

    #region Coroutine

    private IEnumerator FlashCoroutine(Color color, float alpha, float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float curveAlpha = flashCurve.Evaluate(t) * alpha;

            _image.color = new Color(color.r, color.g, color.b, curveAlpha);
            yield return null;
        }

        _image.color = new Color(color.r, color.g, color.b, 0f);
    }

    #endregion

    private void OnDestroy()
    {
        StopAllCoroutines();
    }
}