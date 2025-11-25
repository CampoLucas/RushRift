using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

[DisallowMultipleComponent]
public class HoverVideoFade : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [Tooltip("RawImage that displays the video RenderTexture.")]
    [SerializeField] private RawImage videoImage;

    [Tooltip("VideoPlayer that renders into the RawImage's texture.")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Tooltip("Optional: base/static image under the video, for cross-fade.")]
    [SerializeField] private Graphic baseImage;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 0.3f;
    [Tooltip("If true, baseImage alpha will be inverted (1 - videoAlpha).")]
    [SerializeField] private bool crossFadeBaseImage = false;

    private Coroutine _fadeRoutine;
    private float _currentAlpha; // 0 = hidden, 1 = fully visible

    private void Awake()
    {
        // Ensure initial state: video hidden
        _currentAlpha = 0f;
        ApplyAlpha(_currentAlpha);

        // Make sure the videoImage is enabled (we control only alpha)
        if (videoImage != null)
            videoImage.enabled = true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        StartFade(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StartFade(false);
    }

    private void StartFade(bool visible)
    {
        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);

        _fadeRoutine = StartCoroutine(FadeRoutine(visible));
    }

    private IEnumerator FadeRoutine(bool visible)
    {
        float start = _currentAlpha;
        float end = visible ? 1f : 0f;

        if (visible && videoPlayer != null)
        {
            // Always restart video on hover
            videoPlayer.Stop();
            videoPlayer.time = 0;
            videoPlayer.frame = 0;
            videoPlayer.Play();
        }

        float time = 0f;
        float duration = Mathf.Max(0.0001f, fadeDuration);

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);
            _currentAlpha = Mathf.Lerp(start, end, t);
            ApplyAlpha(_currentAlpha);
            yield return null;
        }

        _currentAlpha = end;
        ApplyAlpha(_currentAlpha);

        if (!visible && videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        _fadeRoutine = null;
    }

    private void ApplyAlpha(float alpha)
    {
        if (videoImage != null)
        {
            var c = videoImage.color;
            c.a = alpha;
            videoImage.color = c;
        }

        if (crossFadeBaseImage && baseImage != null)
        {
            var c = baseImage.color;
            c.a = 1f - alpha;
            baseImage.color = c;
        }
    }
}