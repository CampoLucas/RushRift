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

    [Tooltip("Max alpha value of the flash.")]
    [SerializeField, Range(0f, 1f)] private float flashAlpha = 1f;

    [Tooltip("Total duration of the flash effect.")]
    [SerializeField, Range(0f, 5f)] private float flashDuration = 0.2f;

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
    

    public void TriggerFlash(string hexColor)
    {
        if (ColorUtility.TryParseHtmlString(hexColor, out Color color))
        {
            TriggerFlash(color);
        }
        
        else
        {
            Debug.LogWarning($"Invalid hex color: {hexColor}");
        }
    }

    private void TriggerFlash(Color color)
    {
        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
        }

        _flashCoroutine = StartCoroutine(FlashCoroutine(color));
    }


    #endregion

    #region Coroutine

    private IEnumerator FlashCoroutine(Color color)
    {
        float timer = 0f;

        while (timer < flashDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / flashDuration);
            float alpha = flashCurve.Evaluate(t) * flashAlpha;

            _image.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        // Optional: fade to 0 alpha instead of disabling the Image
        _image.color = new Color(color.r, color.g, color.b, 0f);
    }
    #endregion
}