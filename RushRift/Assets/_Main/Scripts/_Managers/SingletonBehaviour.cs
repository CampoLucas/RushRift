using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// Generic base class for MonoBehaviour singletons that supports runtime creation,
    /// optional persistence, and async-safe access.
    /// </summary>
    /// <typeparam name="TBehaviour">The type of the singleton MonoBehaviour.</typeparam>
    public abstract class SingletonBehaviour<TBehaviour> : MonoBehaviour, IDisposable
        where TBehaviour : MonoBehaviour
    {
        public static NullCheck<TBehaviour> Instance
        {
            get
            {
                if (!Usable) return null;

                if (!_instance)
                {
                    var existing = FindObjectOfType<TBehaviour>(true);
                    if (existing)
                    {
                        _instance = existing;
                    }
                    else if (GetCreateFlag())
                    {
                        var go = new GameObject(typeof(TBehaviour).Name);
                        _instance = go.AddComponent<TBehaviour>();
                    }
                }

                return _instance;
            }
        }
        
        protected static bool Usable => _instance && !_disposedInstance && !_quiting;
        
        protected static NullCheck<TBehaviour> _instance;
        private static bool _disposedInstance;
        private static bool _quiting;

        private void Awake()
        {
            // If an instance already exists and it's not this one, destroy 
            if (Usable && _instance.Get() != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this as TBehaviour;
            _disposedInstance = false;
            _quiting = false;

            if (_instance.TryGet(out var instance) && DontDestroy())
            {
                if (transform.parent)
                {
                    transform.parent = null;
                }
                
                DontDestroyOnLoad(gameObject);
            }
            
            OnAwake();
        }
        
        public static async UniTask<NullCheck<TBehaviour>> GetAsync()
        {
            await UniTask.WaitUntil(() => Usable);
            return _instance;
        }
        
        public static async UniTask<NullCheck<TBehaviour>> GetAsync(CancellationToken ct)
        {
            await UniTask.WaitUntil(() => Usable, cancellationToken: ct);
            return _instance;
        }

        public static void GetAsync(out CancellationTokenSource cts, out UniTask<NullCheck<TBehaviour>> task)
        {
            cts = new CancellationTokenSource();
            task = UniTask.Create(Factory(cts));
        }

        private static Func<UniTask<NullCheck<TBehaviour>>> Factory(CancellationTokenSource cts)
        {
            return async () =>
            {
                await UniTask.WaitUntil(() => Usable, cancellationToken: cts.Token);
                cts.Token.ThrowIfCancellationRequested();
                return _instance.Get();
            };
        }

        protected virtual void OnAwake() { }
        protected abstract bool CreateIfNull();
        protected abstract bool DontDestroy();
        protected virtual void OnDisposeInstance() { }
        protected virtual void OnDisposeNotInstance() { }
        
        private static bool GetCreateFlag()
        {
            // try to read from an existing scriptable instance if present, default true
            var t = typeof(TBehaviour);
            var any = FindObjectOfType<TBehaviour>(true);
            if (any && any is SingletonBehaviour<TBehaviour> s)
                return s.CreateIfNull();
            return true;
        }

        public void Dispose()
        {
            if (_instance.Get() == this && !_disposedInstance)
            {
                _disposedInstance = true;
                OnDisposeInstance(); // dispose stuff that the instance only has.
                _instance = null;
            }
            OnDisposeNotInstance(); // dispose serialized references and stuff the class might have.
        }

        private void OnDestroy()
        {
            Dispose();
        }

        private void OnApplicationQuit()
        {
            _quiting = true;
            Dispose();
        }
    }
}