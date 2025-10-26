using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.DesignPatterns.Observers;
using Game.Levels;
using Game.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game
{
    public enum LoadResult
    {
        Ok = 200,
        Cancelled = 499,
        InvalidSession = 400,
        MissingLevel = 404,
        ManagersNotFound = 503,
        SceneLoadFailed = 520,
        Exception = 500,
    }
    
    public static class GameEntry
    {
        public static LoadingState LoadingState { get; private set; } = LoadingState.Create();
        public const string MAIN_SCENE = "MainScene";

        private static CancellationTokenSource _cts;

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
        
        public static async UniTask<LoadResult> TryAwaitLoadSessionAsync(
            GameSessionSO session, 
            bool mainSceneAdditive = false,
            CancellationToken ct = default)
        {
            if (session.IsNullOrMissingReference()) return LoadResult.InvalidSession;
            if (session.Level.IsNullOrMissingReference()) return LoadResult.MissingLevel;

            return await TryAwaitLoad(session, session.Level, mainSceneAdditive, ct);
        }
        
        public static async UniTask<LoadResult> TryAwaitLoadLevelAsync(BaseLevelSO level, bool mainSceneAdditive = false)
        {
            var session = GameSessionSO.GetOrCreate(GlobalLevelManager.CurrentSession, null, level);
            return await TryAwaitLoadSessionAsync(session, mainSceneAdditive);
        }

        private static async UniTask<LoadResult> TryAwaitLoad(
            GameSessionSO session, 
            BaseLevelSO level, 
            bool mainSceneAdditive = false,
            CancellationToken ct = default)
        {
            // Set up a linked CTS so we can cancel it
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _cts = linked;

            try
            {
                SetLoading(true);
                // Only after mangers are ready, notify about preload 
                NotifyPreload(level);

                // Ensure MainScene is loaded and managers are alive
                var mainScene = SceneHandler.GetSceneByName(MAIN_SCENE);
                if (!mainScene.isLoaded)
                {
                    var lr = await LoadMainSceneAsync(mainSceneAdditive, linked.Token);
                    if (lr != LoadResult.Ok) return Fail(lr, "Failed to load MainScene.");
                }

                // Wait until critical managers are ready
                var readyManagers = await EnsureManagersReadyAsync(linked.Token);
                if (!readyManagers) return Fail(LoadResult.ManagersNotFound, "Managers not ready.");


                // Bind session & Load
                var sessionRes = await TryAwaitLoadSession(session, linked.Token);
                if (sessionRes != LoadResult.Ok) return Fail(sessionRes, "Failed to bind session.");

                // Call the event when the level is loaded.
                NotifyLoaded(level);

                // Respawn the player
                linked.Token.ThrowIfCancellationRequested();
                await PlayerSpawner.RespawnPlayerAsync(ct);

                // Unload any previous scenes
                await AwaitUnloadPrevScene(linked.Token);

                SetLoading(false);
                NotifyReady(level);
                return LoadResult.Ok;
            }
            catch (OperationCanceledException)
            {
                return Fail(LoadResult.Cancelled, "Load cancelled.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameEntry] Unhandled load error: {ex}");
                return Fail(LoadResult.Exception, "Unexpected error.");
            }
            finally
            {
                _cts = null;
            }
            
            LoadResult Fail(LoadResult code, string msg)
            {
                LoadingState.SetLoading(false);
                ForceReturnToMainMenu(code, msg);
                return code;
            }
        }

        #region Try Catch tests

        private static void SetLoading(bool isLoading)
        {
#if true
            LoadingState.SetLoading(isLoading);
#else
            try
            {
                LoadingState.SetLoading(isLoading);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameEntry] SetLoading({isLoading}) threw: {e}");
                throw;
            }
#endif
        }

        private static void NotifyPreload(BaseLevelSO level)
        {
#if true
            LoadingState.NotifyPreload(level);
#else
            try
            {
                LoadingState.NotifyPreload(level);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameEntry] NotifyPreload threw: {e}");
                throw;
            }
#endif
            
        }

        private static void NotifyLoaded(BaseLevelSO level)
        {
#if true
            LoadingState.NotifyLoaded(level);
#else
            try
            {
                LoadingState.NotifyLoaded(level);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameEntry] NotifyLoaded threw: {e}");
                throw;
            }
#endif
        }
        
        private static void NotifyReady(BaseLevelSO level)
        {
#if true
            LoadingState.NotifyReady(level);
#else
            try
            {
                LoadingState.NotifyReady(level);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameEntry] NotifyReady threw: {e}");
                throw;
            }
#endif
        }

        #endregion

        private static async UniTask<bool> EnsureManagersReadyAsync(CancellationToken ct)
        {
            // Await GlobalLevelManager
            var managerCheck = await GlobalLevelManager.GetAsync(ct);
            if (!managerCheck) return false;

            var spawner = await PlayerSpawner.GetAsync(ct);
            if (!spawner) return false;
            
            return true;
        }
        
        private static async UniTask AwaitUnloadPrevScene(CancellationToken ct)
        {
            // Unload the previous scene (Hub, menu, etc.)
            var active = SceneHandler.GetActiveScene();
            if (active.name == MAIN_SCENE) return;
            
            var unloadPrev = SceneHandler.UnloadSceneAsync(active);
            if (unloadPrev != null)
            {
                await unloadPrev.ToUniTask(cancellationToken: ct);
            }
        }

        private static async UniTask<LoadResult> TryAwaitLoadSession(GameSessionSO session, CancellationToken ct)
        {
            var managerValue = await GlobalLevelManager.GetAsync(ct);
            if (!managerValue.TryGet(out var manager))
            {
                return LoadResult.ManagersNotFound;
            }

            manager.SetSession(session);
            
            // Tell the manager which level to load
            ct.ThrowIfCancellationRequested();
            await manager.WaitLoadLevel(session.Level);
            return LoadResult.Ok;
        }

        private static async UniTask<LoadResult> LoadMainSceneAsync(bool additive, CancellationToken ct)
        {
            if (additive)
            {
                var op = SceneHandler.LoadSceneAsync(MAIN_SCENE, LoadSceneMode.Additive);
                if (op == null) return LoadResult.SceneLoadFailed;

                await op.ToUniTask(cancellationToken: ct);
                return LoadResult.Ok;
            }
            
            SceneHandler.LoadScene(MAIN_SCENE);
            return LoadResult.Ok;
        }

        private static void ForceReturnToMainMenu(LoadResult code, string reason)
        {
            Debug.LogError($"[GameEntry] Load fail ({(int)code}): {reason}");
            
            // Ensure we are on Main Menu
            var main = SceneHandler.GetSceneByName(SceneHandler.MainMenuName);
            if (!main.isLoaded)
            {
                SceneHandler.LoadScene(SceneHandler.MainMenuName);
            }
            
            // ToDo: show pop up.
        }
    }

    public class LoadingState
    {
        public bool Loading { get; private set; }
        
        private readonly ISubject<bool> _onLoading;
        private readonly ISubject<BaseLevelSO> _onLevelPreload;
        private readonly ISubject<BaseLevelSO> _onLevelLoaded;
        private readonly ISubject<BaseLevelSO> _onLevelReady;
        
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
        public bool AttachOnLoading(DesignPatterns.Observers.IObserver<bool> observer, bool disposeOnDetach = false)
        {
            return _onLoading.Attach(observer, disposeOnDetach);
        }
        
        
        /// <summary>
        /// It is called at the start when loading a level.
        /// </summary>
        /// <param name="observer"></param>
        /// <param name="disposeOnDetach"></param>
        /// <returns></returns>
        public bool AttachOnPreload(DesignPatterns.Observers.IObserver<BaseLevelSO> observer, bool disposeOnDetach = false)
        {
            return _onLevelPreload.Attach(observer, disposeOnDetach);
        }
        
        /// <summary>
        /// Its called when the level is loaded.
        /// </summary>
        /// <param name="observer"></param>
        /// <param name="disposeOnDetach"></param>
        /// <returns></returns>
        public bool AttachOnLoad(DesignPatterns.Observers.IObserver<BaseLevelSO> observer, bool disposeOnDetach = false)
        {
            return _onLevelLoaded.Attach(observer, disposeOnDetach);
        }

        /// <summary>
        /// It is called after everything finished loading and being set up.
        /// </summary>
        /// <param name="observer"></param>
        /// <param name="disposeOnDetach"></param>
        /// <returns></returns>
        public bool AttachOnReady(DesignPatterns.Observers.IObserver<BaseLevelSO> observer, bool disposeOnDetach = false)
        {
            return _onLevelReady.Attach(observer, disposeOnDetach);
        }
        
        public bool DetachOnLoading(DesignPatterns.Observers.IObserver<bool> observer)
        {
            return _onLoading.Detach(observer);
        }
        
        public bool DetachOnPreload(DesignPatterns.Observers.IObserver<BaseLevelSO> observer)
        {
            return _onLevelPreload.Detach(observer);
        }
        
        public bool DetachOnLoad(DesignPatterns.Observers.IObserver<BaseLevelSO> observer)
        {
            return _onLevelLoaded.Detach(observer);
        }
        
        public bool DetachOnReady(DesignPatterns.Observers.IObserver<BaseLevelSO> observer)
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