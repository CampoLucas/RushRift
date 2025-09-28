using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class DisplayElement : MonoBehaviour
{
    private const int MAX_SLIDES = 100;
    
    [System.Serializable]
    public struct SlideAnim
    {
        public float duration;
        public float delay;
        public float endPosition;
        public AnimationCurve curve;
    }
    
    [Header("Settings")]
    [SerializeField] private float horizontalOffset;
    
    [Header("References")]
    [SerializeField] private RectTransform pivot;

    [Header("Animation")]
    [SerializeField] private SlideAnim[] anims;

    [FormerlySerializedAs("onComplete")]
    [Header("Events")]
    [SerializeField] private UnityEvent onCompleteAnim = new UnityEvent();

    private Coroutine _animCoroutine;

    private void OnEnable()
    {
        if (onCompleteAnim == null) onCompleteAnim = new UnityEvent();
    }

    public void SetXPos(float pos)
    {
        if (!pivot)
        {
#if UNITY_EDITOR
            Debug.LogError("ERROR: The DisplayContainer the pivot is missing, cannot SetXPos", this);
#endif
            return;
        }
        
        var targetX = pos + horizontalOffset;
        pivot.anchoredPosition = new Vector2(targetX, pivot.anchoredPosition.y);
    }

    public void DoAnim(float startPos, float delay = 0f, Action onComplete = null)
    {
        StopAnim();

        if (!pivot)
        {
#if UNITY_EDITOR
            Debug.LogError("ERROR: The DisplayContainer the pivot is missing, cannot run animation", this);
#endif
            OnAnimComplete(onComplete);
            return;
        }
        
        if (anims == null || anims.Length == 0)
        {
#if UNITY_EDITOR
            Debug.LogWarning("ERROR: The DisplayContainer has no animation", this);
#endif
            OnAnimComplete(onComplete);
            return;
        }
        
        _animCoroutine = StartCoroutine(PlayRecursive(anims, 0, startPos, delay, onComplete));
    }

    private void OnAnimComplete(in Action onComplete)
    {
        onComplete?.Invoke();
        onCompleteAnim?.Invoke();
        _animCoroutine = null;
    }

    private IEnumerator PlayRecursive(SlideAnim[] slides, int index, float startPos, float delay, Action onComplete)
    {
        if (index >= slides.Length)
        {
            OnAnimComplete(onComplete);
            yield break;
        }

        if (delay > 0 && !float.IsNaN(delay) && !float.IsInfinity(delay))
            yield return new WaitForSeconds(delay);

        var slide = slides[index];

        yield return SlideRoutine(slide.duration, slide.delay, startPos, slide.endPosition, slide.curve, (endPos) =>
        {
            StartCoroutine(PlayRecursive(slides, index + 1, endPos, 0, onComplete));
        });
    }

    private IEnumerator SlideRoutine(float duration, float delay, float start, float end, AnimationCurve curve, Action<float> onCompleted)
    {
        SetXPos(start);
        
        if (delay > 0) yield return new WaitForSeconds(delay);
        
        if (duration <= 0)
        {
            SetXPos(end);
            onCompleted?.Invoke(end);
            yield break;
        }
        
        var time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            
            var t = Mathf.Clamp01(time / duration);
            
            // Use curve if provided, otherwise linear
            float curvedT = (curve != null && curve.length > 0) ? curve.Evaluate(t) : t;
            
            //var x = Mathf.Lerp(start, end, t);
            var x = Mathf.LerpUnclamped(start, end, curvedT);
            SetXPos(x);

            yield return null;
        }
        
        SetXPos(end);
        onCompleted?.Invoke(end);
    }

    /// <summary>
    /// Stops the current animation immediately.
    /// </summary>
    public void StopAnim()
    {
        if (_animCoroutine != null)
        {
            StopCoroutine(_animCoroutine);
            _animCoroutine = null;
        }
    }

    private void OnValidate()
    {
        if (!pivot)
        {
#if UNITY_EDITOR
            Debug.LogWarning("WARNING: Pivot reference missing in the DisplayContainer.", gameObject);
#endif
        }
        else
        {
            SetXPos(0);
        }
    }

    private void OnDisable()
    {
        StopAnim();
    }

    private void OnDestroy()
    {
        pivot = null;
        anims = null;
        onCompleteAnim = null;
    }
}
