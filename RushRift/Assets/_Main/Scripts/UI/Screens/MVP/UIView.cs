using System;
using System.Collections;
using Game.DesignPatterns.Observers;
using UnityEngine;

namespace Game.UI.Screens
{
    [RequireComponent(typeof(CanvasGroup), typeof(Canvas))]
    public class UIView : MonoBehaviour, IDisposable
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Canvas canvas;
        
        private bool _enabled;
        private bool _started;
        
        private Coroutine _coroutine;

        protected virtual void Awake()
        {
            if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
            if (!canvas) canvas = GetComponent<Canvas>();
        }

        public void Show()
        {
            canvas.enabled = true;
            canvasGroup.interactable = true;
        }

        public void Hide()
        {
            canvas.enabled = false;
            canvasGroup.interactable = false;
        }
        
        public void FadeIn(float t, float startTime, float duration, ref ISubject onStart, ref ISubject onEnd)
        {
            var endTime = startTime + duration;

            if (t >= startTime)
            {
                if (!_enabled)
                {
                    _enabled = true;
                    canvasGroup.alpha = 0;
                    Show();
                    
                    onStart.NotifyAll();
                }
                else
                {
                    canvasGroup.alpha = (t - startTime) / duration;
                }
            }
            if (!_started && t >= endTime)
            {
                _started = true;
                canvasGroup.alpha = 1;
                onEnd.NotifyAll();
            }
        }

        public void FadeOut(float t, float startTime, float duration, ref ISubject onStart, ref ISubject onEnd)
        {
            var endTime = startTime + duration;

            if (t >= startTime)
            {
                if (_started)
                {
                    _started = false;
                    onStart.NotifyAll();

                    canvasGroup.alpha = 1;
                }
                else
                {
                    canvasGroup.alpha = 1 - ((t - startTime) / duration);
                }
            }
            if (_enabled && t >= endTime)
            {
                _enabled = false;
                canvasGroup.alpha = 0;
                Hide();
                onEnd.NotifyAll();
            }
        }

        public virtual void Dispose()
        {
            canvasGroup = null;
            if (_coroutine != null) StopCoroutine(_coroutine);
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}