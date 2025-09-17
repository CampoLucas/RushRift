using System;
using Game.DesignPatterns.Observers;
using UnityEngine;

namespace Game
{
    public class EffectManager : MonoBehaviour
    {
        private static EffectManager _instance;
        private static bool _disposed;
        private ISubject<float, float> _shakeSubject;
        private ISubject<float, float> _screenBlurSubject;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _disposed = false;
            _instance = this;

            _shakeSubject = new Subject<float, float>(false, true);
            _screenBlurSubject = new Subject<float, float>(false, true);
        }

        /// <summary>
        /// Attaches an observer to the event when the camera should shake.
        /// All observers that are attached, will be disposed when the Manager is destroyed.
        /// </summary>
        /// <param name="observer"></param>
        public static void AttachShake(DesignPatterns.Observers.IObserver<float, float> observer)
        {
            if (!_instance || _disposed) return;
            _instance._shakeSubject.Attach(observer);
        }
        
        /// <summary>
        /// Detaches an observer from the camera shake event.
        /// </summary>
        /// <param name="observer"></param>
        public static void DetachShake(DesignPatterns.Observers.IObserver<float, float> observer)
        {
            if (!_instance || _disposed) return;
            _instance._shakeSubject.Detach(observer);
        }
        
        /// <summary>
        /// Attaches an observer to the event when the screen should blur.
        /// All observers that are attached, will be disposed when the Manager is destroyed.
        /// </summary>
        /// <param name="observer"></param>
        public static void AttachBlur(DesignPatterns.Observers.IObserver<float, float> observer)
        {
            if (!_instance || _disposed) return;
            _instance._screenBlurSubject.Attach(observer);
        }
        
        /// <summary>
        /// Detaches an observer from the screen blur event.
        /// </summary>
        /// <param name="observer"></param>
        public static void DetachBlur(DesignPatterns.Observers.IObserver<float, float> observer)
        {
            if (!_instance || _disposed) return;
            _instance._screenBlurSubject.Detach(observer);
        }
        
        public static void CameraShake(float duration, float magnitude)
        {
            if (!_instance || _disposed) return;
            _instance._shakeSubject.NotifyAll(duration, magnitude);
        }

        public static void ScreenBlur(float duration, float magnitude)
        {
            if (!_instance || _disposed) return;
            _instance._screenBlurSubject.NotifyAll(duration, magnitude);
        }

        

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _disposed = true;
                _instance = null;
            }
            _shakeSubject.DetachAll();
            _shakeSubject = null;
            
            _screenBlurSubject.DetachAll();
            _screenBlurSubject = null;
        }
    }
}