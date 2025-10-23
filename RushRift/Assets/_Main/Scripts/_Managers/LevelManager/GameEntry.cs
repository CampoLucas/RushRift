using Cysharp.Threading.Tasks;
using Game.DesignPatterns.Observers;
using Game.Levels;
using Game.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game
{
    public static class GameEntry
    {
        public static LoadingState LoadingState { get; private set; } = LoadingState.Create();
        public static BaseLevelSO PendingLevel;
        public const string MAIN_SCENE = "MainScene";


        public static async void LoadSessionAsync(GameSessionSO session, bool mainSceneAdditive = false)
        {
            await TryAwaitLoadSessionAsync(session, mainSceneAdditive);
        }
        
        public static async void LoadSessionAsync(GameModeSO gameMode, BaseLevelSO level, bool mainSceneAdditive = false)
        {
            var session = ScriptableObject.CreateInstance<GameSessionSO>();
            session.Initialize(gameMode, level);
            
            await TryAwaitLoadSessionAsync(session, mainSceneAdditive);
        }

        public static async void LoadLevelAsync(BaseLevelSO level, bool mainSceneAdditive = false)
        {
            await TryAwaitLoadLevelAsync(level, mainSceneAdditive);
        }
        
        public static async UniTask AwaitOnMainSceneLoaded(GlobalLevelManager manager)
        {
            if (PendingLevel == null)
                return;

            await PendingLevel.LoadAsync(manager);
            PendingLevel = null;
        }
        
        public static async UniTask<bool> TryAwaitLoadSessionAsync(GameSessionSO session, bool mainSceneAdditive = false)
        {
            if (!session)
            {
                Debug.LogError("ERROR: Couldn't load the session. Reason: session is null.");
                return false;
            }

            if (!session.Level)
            {
                Debug.LogError("ERROR: Couldn't load the session. Reason: level is null.");
                return false;
            }
            
            if (!await TryAwaitLoad(session, session.Level, mainSceneAdditive))
            {
                Debug.LogError("ERROR: Couldn't load the session.");
                return false;
            }

            return true;
        }
        
        public static async UniTask<bool> TryAwaitLoadLevelAsync(BaseLevelSO level, bool mainSceneAdditive = false)
        {
            var session = GameSessionSO.GetOrCreate(GlobalLevelManager.CurrentSession, null, level);
            
            if (!await TryAwaitLoadSessionAsync(session, mainSceneAdditive))
            {
                return false;
            }

            return true;
        }

        private static async UniTask<bool> TryAwaitLoad(GameSessionSO session, BaseLevelSO level, bool mainSceneAdditive = false)
        {
            LoadingState.SetLoading(true);
            LoadingState.NotifyPreload(level);
            
            var mainScene = SceneHandler.GetSceneByName(MAIN_SCENE);

            // Load the main scene if it isn't loaded.
            if (!mainScene.isLoaded)
            {
                await LoadMainSceneAsync(mainSceneAdditive);
            }

            // If it is a session, load it
            if (!await TryAwaitLoadSession(session))
            {
                return false;
            }
            
            // Call the event when the level is loaded.
            LoadingState.NotifyLoaded(level);
            
            // Respawn the player
            await PlayerSpawner.RespawnPlayerAsync();
            // Unload any previous scenes
            await AwaitUnloadPrevScene();
            
            LoadingState.SetLoading(false);
            LoadingState.NotifyReady(level);

            return true;
        }

        private static async UniTask AwaitUnloadPrevScene()
        {
            // Unload the previous scene (Hub, menu, etc.)
            var active = SceneHandler.GetActiveScene();
            if (active.name != MAIN_SCENE)
            {
                var unloadPrev = SceneHandler.UnloadSceneAsync(active);
                if (unloadPrev != null)
                {
                    await unloadPrev.ToUniTask();
                }
            }
        }

        private static async UniTask<bool> TryAwaitLoadLevel(BaseLevelSO level)
        {
            var managerValue = await TryAwaitForManager();
            if (!managerValue.TryGet(out var manager))
            {
                return false;
            }
            
            // Tell the manager which level to load
            await manager.WaitLoadLevel(level);
            return true;
        }

        private static async UniTask<bool> TryAwaitLoadSession(GameSessionSO session)
        {
            var managerValue = await TryAwaitForManager();
            if (!managerValue.TryGet(out var manager))
            {
                return false;
            }

            manager.SetSession(session);
            
            // Tell the manager which level to load
            await manager.WaitLoadLevel(session.Level);
            return true;
        }

        private static async UniTask<NullCheck<GlobalLevelManager>> TryAwaitForManager()
        {
            // Find the level manager in the MainScene
            var managerCheck = await GlobalLevelManager.GetAsync();
            if (!managerCheck.TryGet(out var manager))
            {
                Debug.LogError($"{typeof(GlobalLevelManager)} was not found.");

                return new NullCheck<GlobalLevelManager>();
            }

            return manager;
        }

        private static async UniTask LoadMainSceneAsync(bool additive)
        {
            if (additive)
            {
                // If the main scene is not loaded, load it additively.
                var loadMain = SceneHandler.LoadSceneAsync(MAIN_SCENE, LoadSceneMode.Additive);
                if (loadMain == null)
                {
#if UNITY_EDITOR
                    Debug.LogError($"ERROR: loadMain is null.");    
#endif
                    return;
                }
                await loadMain.ToUniTask();
            }
            else
            {
                SceneHandler.LoadScene(MAIN_SCENE);
            }
        }
    }

    public struct LoadingState
    {
        public bool Loading { get; private set; }
        
        public readonly ISubject<bool> _onLoading;
        public readonly ISubject<BaseLevelSO> _onLevelPreload;
        public readonly ISubject<BaseLevelSO> _onLevelLoaded;
        public readonly ISubject<BaseLevelSO> _onLevelReady;
        
        public LoadingState(ISubject<bool> onLoading, ISubject<BaseLevelSO> onLevelPreloaded, ISubject<BaseLevelSO> onLevelLoaded, ISubject<BaseLevelSO> onLevelReady)
        {
            Loading = false;
            _onLoading = onLoading;
            _onLevelPreload = onLevelPreloaded;
            _onLevelLoaded = onLevelLoaded;
            _onLevelReady = onLevelReady;
        }

        public static LoadingState Create()
        {
            return new LoadingState(new Subject<bool>(), new Subject<BaseLevelSO>(), new Subject<BaseLevelSO>(), new Subject<BaseLevelSO>());
        }

        public void SetLoading(bool isLoading)
        {
            if (Loading == isLoading)
            {
                return;
            }

            Loading = isLoading;
            _onLoading.NotifyAll(isLoading);
        }

        public void NotifyPreload(BaseLevelSO level)
        {
            _onLevelPreload.NotifyAll(level);
        }
        
        public void NotifyLoaded(BaseLevelSO level)
        {
            _onLevelLoaded.NotifyAll(level);
        }
        
        public void NotifyReady(BaseLevelSO level)
        {
            _onLevelReady.NotifyAll(level);
        }
        

        #region Event Management

        /// <summary>
        /// Attach an observer for when the Loading subject is called.
        /// It's called at the very beginning and end when loading the level.
        /// </summary>
        /// <param name="observer"></param>
        /// <param name="disposeOnDetach"></param>
        /// <returns></returns>
        public bool AttachOnLoading(IObserver<bool> observer, bool disposeOnDetach = false)
        {
            return _onLoading.Attach(observer, disposeOnDetach);
        }
        
        
        /// <summary>
        /// It is called at the start when loading a level.
        /// </summary>
        /// <param name="observer"></param>
        /// <param name="disposeOnDetach"></param>
        /// <returns></returns>
        public bool AttachOnPreload(IObserver<BaseLevelSO> observer, bool disposeOnDetach = false)
        {
            return _onLevelPreload.Attach(observer, disposeOnDetach);
        }
        
        /// <summary>
        /// Its called when the level is loaded.
        /// </summary>
        /// <param name="observer"></param>
        /// <param name="disposeOnDetach"></param>
        /// <returns></returns>
        public bool AttachOnLoad(IObserver<BaseLevelSO> observer, bool disposeOnDetach = false)
        {
            return _onLevelLoaded.Attach(observer, disposeOnDetach);
        }

        /// <summary>
        /// It is called after everything finished loading and being set up.
        /// </summary>
        /// <param name="observer"></param>
        /// <param name="disposeOnDetach"></param>
        /// <returns></returns>
        public bool AttachOnReady(IObserver<BaseLevelSO> observer, bool disposeOnDetach = false)
        {
            return _onLevelReady.Attach(observer, disposeOnDetach);
        }
        
        public bool DetachOnLoading(IObserver<bool> observer)
        {
            return _onLoading.Detach(observer);
        }
        
        public bool DetachOnPreload(IObserver<BaseLevelSO> observer)
        {
            return _onLevelPreload.Detach(observer);
        }
        
        public bool DetachOnLoad(IObserver<BaseLevelSO> observer)
        {
            return _onLevelLoaded.Detach(observer);
        }
        
        public bool DetachOnReady(IObserver<BaseLevelSO> observer)
        {
            return _onLevelReady.Detach(observer);
        }

        public void DetachAll()
        {
            _onLoading.DetachAll();
            _onLevelPreload.DetachAll();
            _onLevelLoaded.DetachAll();
            _onLevelReady.DetachAll();
        }

        #endregion
        
    }
}