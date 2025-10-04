using System;
using Game.DesignPatterns.Observers;
using Game.Utils;
using Game.VFX;
using UnityEngine;

namespace Game
{
    public class EffectManager : MonoBehaviour, IDisposable
    {
        [Header("VFX Pool")]
        [SerializeField] private EffectPool effectPool;
        
        public static bool IsAlive => _instance && !_disposed;
        private static EffectManager _instance;
        private static bool _disposed;
        private ISubject<float, float> _shakeSubject;
        private ISubject<float, float> _screenBlurSubject;
        private IObserver _sceneChangedObserver;
        
        private void Awake()
        {
            if (IsAlive && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _disposed = false;
            _instance = this;

            _shakeSubject = new Subject<float, float>(false, true);
            _screenBlurSubject = new Subject<float, float>(false, true);
            _sceneChangedObserver = new ActionObserver(OnSceneChangedHandler);

        }

        private void Start()
        {
            SceneHandler.OnSceneChanged.Attach(_sceneChangedObserver);
        }

        #region Screen Effects

        /// <summary>
        /// Attaches an observer to the event when the camera should shake.
        /// All observers that are attached, will be disposed when the Manager is destroyed.
        /// </summary>
        /// <param name="observer"></param>
        public static void AttachShake(IObserver<float, float> observer)
        {
            if (!_instance || _disposed) return;
            _instance._shakeSubject.Attach(observer);
        }
        
        /// <summary>
        /// Detaches an observer from the camera shake event.
        /// </summary>
        /// <param name="observer"></param>
        public static void DetachShake(IObserver<float, float> observer)
        {
            if (!_instance || _disposed) return;
            _instance._shakeSubject.Detach(observer);
        }
        
        /// <summary>
        /// Attaches an observer to the event when the screen should blur.
        /// All observers that are attached, will be disposed when the Manager is destroyed.
        /// </summary>
        /// <param name="observer"></param>
        public static void AttachBlur(IObserver<float, float> observer)
        {
            if (!_instance || _disposed) return;
            _instance._screenBlurSubject.Attach(observer);
        }
        
        /// <summary>
        /// Detaches an observer from the screen blur event.
        /// </summary>
        /// <param name="observer"></param>
        public static void DetachBlur(IObserver<float, float> observer)
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

        #endregion

        #region VFX Pool

        public static bool TryGetVFX(VFXPrefabID id, VFXEmitterParams vfxParams, out EffectEmitter emitter)
        {
            if (!_instance || _disposed || _instance.effectPool == null)
            {
                emitter = null; 
                return false;
            }

            if (_instance.effectPool.TryGetVFX(id, vfxParams, out emitter))
            {
#if UNITY_EDITOR
                Debug.Log($"LOG: TryGetVFX: Success || VFX: {emitter.gameObject.name}");
#endif
                return true;
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning("WARNING: TryGetVFX: Failure");
#endif
                return false;
            }
        }

        #endregion

        private void OnSceneChangedHandler()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (_sceneChangedObserver != null)
            {
                SceneHandler.OnSceneChanged.Detach(_sceneChangedObserver);
                _sceneChangedObserver.Dispose();
                _sceneChangedObserver = null;
            }
            
            if (_disposed) return;
            
            if (_instance == this)
            {
                _disposed = true;
                _instance = null;
            }
            _shakeSubject.DetachAll();
            _shakeSubject = null;
            effectPool.Dispose();
            
            _screenBlurSubject.DetachAll();
            _screenBlurSubject = null;
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}