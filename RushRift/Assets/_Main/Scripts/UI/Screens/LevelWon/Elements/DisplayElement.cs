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
        public Vector2 endPosition;
        public AnimationCurve curveX;
        public AnimationCurve curveY;
    }
    
    [Header("Settings")]
    [SerializeField] private Vector2 offset;
    [SerializeField] private Vector2 defaultPlayPosition;

    [Header("References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform pivot;

    [Header("Animation")]
    [SerializeField] private SlideAnim[] anims;

    [Header("Events")]
    [SerializeField] private UnityEvent onCompleteAnim = new UnityEvent();

    private Coroutine _animCoroutine;

    private void OnEnable()
    {
        if (onCompleteAnim == null) onCompleteAnim = new UnityEvent();
        
    }

    private void Awake()
    {
        if (canvas) canvas.enabled = false;
    }

    public void SetPosition(Vector2 pos)
    {
        if (!pivot)
        {
#if UNITY_EDITOR
            Debug.LogError("ERROR: The DisplayContainer the pivot is missing, cannot SetXPos", this);
#endif
            return;
        }
        
        pivot.anchoredPosition = pos + offset;
    }

    public void Play()
    {
        if (canvas) canvas.enabled = true;
        DoAnim(defaultPlayPosition);
    }
    
    public void Stop()
    {
        if (canvas) canvas.enabled = true;
        
        if (_animCoroutine != null)
        {
            StopCoroutine(_animCoroutine);
            _animCoroutine = null;
        }
        
        // Instantly set to final state
        if (anims != null && anims.Length > 0)
            SetPosition(anims[^1].endPosition);

        onCompleteAnim?.Invoke();
    }

    public void DoAnim(Vector2 startPos, float delay = 0f, Action onComplete = null)
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
        
        SetPosition(startPos);
        
        _animCoroutine = StartCoroutine(PlayRecursive(anims, 0, startPos, delay, onComplete));
    }

    private void OnAnimComplete(in Action onComplete)
    {
        onComplete?.Invoke();
        onCompleteAnim?.Invoke();
        _animCoroutine = null;
    }

    private IEnumerator PlayRecursive(SlideAnim[] slides, int index, Vector2 startPos, float delay, Action onComplete)
    {
        var maxSlides = index >= MAX_SLIDES;
        if (index >= slides.Length || maxSlides)
        {
            if (maxSlides)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"WARNING: Maximum slides ({MAX_SLIDES}) in DisplayElement reached.", this);
#endif
            }
            
            OnAnimComplete(onComplete);
            yield break;
        }

        if (delay > 0 && !float.IsNaN(delay) && !float.IsInfinity(delay))
            yield return new WaitForSeconds(delay);

        var slide = slides[index];

        var sDur = slide.duration;
        var sDelay = slide.delay;
        var curveX = slide.curveX;
        var curveY = slide.curveY;
        var end = slide.endPosition;
        
        yield return SlideRoutine(sDur, sDelay, startPos, end, curveX, curveY, (endPos) =>
        {
            _animCoroutine = StartCoroutine(PlayRecursive(slides, index + 1, endPos, 0, onComplete));
        });
    }

    private IEnumerator SlideRoutine(float duration, float delay, Vector2 start, Vector2 end, AnimationCurve curveX, 
        AnimationCurve curveY, Action<Vector2> onCompleted)
    {
        SetPosition(start);
        
        if (delay > 0) yield return new WaitForSeconds(delay);
        
        if (duration <= 0)
        {
            SetPosition(end);
            onCompleted?.Invoke(end);
            yield break;
        }
        
        var time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            
            var t = Mathf.Clamp01(time / duration);
            
            // Use curve if provided, otherwise linear
            var xCurvedT = curveX is { length: > 0 } ? curveX.Evaluate(t) : t;
            var yCurvedT = curveY is { length: > 0 } ? curveY.Evaluate(t) : t;
            
            //var x = Mathf.Lerp(start, end, t);
            var pos = new Vector2(Mathf.LerpUnclamped(start.x, end.x, xCurvedT), 
                Mathf.LerpUnclamped(start.y, end.y, yCurvedT));
            SetPosition(pos);

            yield return null;
        }
        
        SetPosition(end);
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
            SetPosition(Vector2.zero);
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
