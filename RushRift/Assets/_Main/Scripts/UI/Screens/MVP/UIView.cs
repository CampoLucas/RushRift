using System;
using System.Collections;
using Game.DesignPatterns.Observers;
using UnityEngine;

namespace Game.UI.Screens
{
    [RequireComponent(typeof(CanvasGroup))]
    public class UIView : MonoBehaviour, IDisposable
    {
        [SerializeField] private CanvasGroup canvasGroup;
        
        private bool _enabled;
        private bool _started;
        
        private Coroutine _coroutine;

        protected virtual void Awake()
        {
            if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
        
        public void FadeIn(float t, float startTime, float duration, ref ISubject onStart, ref ISubject onEnd)
        {
            var endTime = startTime + duration;

            if (t > startTime && t < endTime)
            {
                if (!_enabled)
                {
                    _enabled = true;
                    canvasGroup.alpha = 0;
                    Show();
                    
                    onStart.NotifyAll();
                    
                    return;
                }

                canvasGroup.alpha = (t - startTime) / duration;
            }
            else if (!_started && t >= endTime)
            {
                _started = true;
                canvasGroup.alpha = 1;
                onEnd.NotifyAll();
            }
        }

        public void FadeOut(float t, float startTime, float duration, ref ISubject onStart, ref ISubject onEnd)
        {
            Debug.Log("FadeOUTTT");
            var endTime = startTime + duration;

            if (t > startTime && t < endTime)
            {
                if (_started)
                {
                    _started = false;
                    onStart.NotifyAll();

                    canvasGroup.alpha = 1;
                    return;
                }

                canvasGroup.alpha = 1 - ((t - startTime) / duration);
            }
            else if (_enabled && t >= endTime)
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