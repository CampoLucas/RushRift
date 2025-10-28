using System;
using Game.DesignPatterns.Observers;
using Game.Utils;
using Game.VFX;
using MyTools.Global;
using UnityEngine;
using Logger = MyTools.Global.Logger;

namespace Game
{
    public sealed class EffectManager : SingletonBehaviour<EffectManager>, IDisposable
    {
        [Header("VFX Pool")]
        [SerializeField] private EffectPool effectPool;
        
        //public static bool IsAlive => _instance && !_disposed;
        //private static bool _disposed;
        private ISubject<float, float> _shakeSubject;
        private ISubject<float, float> _screenBlurSubject;
        private ActionObserver<bool> _onLoading;
        
        protected override void OnAwake()
        {
            _shakeSubject = new Subject<float, float>(false, true);
            _screenBlurSubject = new Subject<float, float>(false, true);
            _onLoading = new ActionObserver<bool>(OnLoadingHandler);

            GameEntry.LoadingState.AttachOnLoading(_onLoading);
        }

        protected override bool CreateIfNull()
        {
            return false;
        }

        protected override bool DontDestroy()
        {
            return true;
        }

        #region Screen Effects

        /// <summary>
        /// Attaches an observer to the event when the camera should shake.
        /// All observers that are attached, will be disposed when the Manager is destroyed.
        /// </summary>
        /// <param name="observer"></param>
        public static void AttachShake(IObserver<float, float> observer)
        {
            if (!Usable || !_instance.TryGet(out var manager)) return;
            manager._shakeSubject.Attach(observer);
        }
        
        /// <summary>
        /// Detaches an observer from the camera shake event.
        /// </summary>
        /// <param name="observer"></param>
        public static void DetachShake(IObserver<float, float> observer)
        {
            if (!Usable || !_instance.TryGet(out var manager)) return;
            manager._shakeSubject.Detach(observer);
        }
        
        /// <summary>
        /// Attaches an observer to the event when the screen should blur.
        /// All observers that are attached, will be disposed when the Manager is destroyed.
        /// </summary>
        /// <param name="observer"></param>
        public static void AttachBlur(IObserver<float, float> observer)
        {
            if (!Usable || !_instance.TryGet(out var manager)) return;
            manager._screenBlurSubject.Attach(observer);
        }
        
        /// <summary>
        /// Detaches an observer from the screen blur event.
        /// </summary>
        /// <param name="observer"></param>
        public static void DetachBlur(IObserver<float, float> observer)
        {
            if (!Usable || !_instance.TryGet(out var manager)) return;
            manager._screenBlurSubject.Detach(observer);
        }
        
        public static void CameraShake(float duration, float magnitude)
        {
            if (!Usable || !_instance.TryGet(out var manager)) return;
            manager._shakeSubject.NotifyAll(duration, magnitude);
        }

        public static void ScreenBlur(float duration, float magnitude)
        {
            if (!Usable || !_instance.TryGet(out var manager)) return;
            manager._screenBlurSubject.NotifyAll(duration, magnitude);
        }

        #endregion

        #region VFX Pool

        public static bool TryGetVFX(VFXPrefabID id, VFXEmitterParams vfxParams, out EffectEmitter emitter)
        {
            if (!Usable || !_instance.TryGet(out var manager) || manager.effectPool == null)
            {
                emitter = null; 
                return false;
            }

            if (manager.effectPool.TryGetVFX(id, vfxParams, out emitter))
            {
                emitter.transform.parent = manager.transform;
                Logger.Log($"LOG: TryGetVFX: Success || VFX: {emitter.gameObject.name}");
                return true;
            }
            
            Logger.Log("WARNING: TryGetVFX: Failure", logType: LogType.Warning);
            return false;
        }

        #endregion

        private void OnLoadingHandler(bool loading)
        {
            if (loading)
            {
                effectPool.PoolDisableAll();
            }
        }

        protected override void OnDisposeInstance()
        {
            base.OnDisposeInstance();
            if (_onLoading != null)
            {
                GameEntry.LoadingState.DetachOnLoading(_onLoading);
                _onLoading.Dispose();
                _onLoading = null;
            }
            
            _shakeSubject.DetachAll();
            _shakeSubject = null;
            effectPool.Dispose();
            
            _screenBlurSubject.DetachAll();
            _screenBlurSubject = null;
        }

        protected override void OnDisposeNotInstance()
        {
            base.OnDisposeNotInstance();
            effectPool = null;
        }
    }
}