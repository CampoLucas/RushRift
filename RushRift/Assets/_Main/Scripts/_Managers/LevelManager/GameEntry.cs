using System;
using System.Collections.Generic;
using System.Linq;
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
        Exception = 500
    }
    
    public static class GameEntry
    {
        public static LoadingState LoadingState { get; private set; } = LoadingState.Create();
        public const string MAIN_SCENE = "MainScene";

        private static CancellationTokenSource _cts;
        private static bool _isLoadRunning;
        private static readonly List<Scene> _loadedScenes = new();

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

        public static UniTask<LoadResult> LoadLevelAsync(BaseLevelSO level, bool mainSceneAdditive = false)
        {
            return TryAwaitLoadLevelAsync(level, mainSceneAdditive);
        }
        
        public static async UniTask<LoadResult> TryAwaitLoadSessionAsync(
            GameSessionSO session, 
            bool mainSceneAdditive = false,
            CancellationToken ct = default)
        {
            if (session.IsNullOrMissing()) return LoadResult.InvalidSession;
            if (session.Level.IsNullOrMissing()) return LoadResult.MissingLevel;

            return await TryAwaitLoad(session, session.Level, mainSceneAdditive, ct);
        }
        
        public static async UniTask<LoadResult> TryAwaitLoadLevelAsync(BaseLevelSO level, bool mainSceneAdditive = false)
        {
            var session = GameSessionSO.GetOrCreate(GlobalLevelManager.CurrentSession, null, level);
            return await TryAwaitLoadSessionAsync(session, mainSceneAdditive);
        }

        private static async UniTask<LoadResult> TryAwaitLoad(GameSessionSO session, BaseLevelSO level, 
            bool mainSceneAdditive = false, CancellationToken ct = default)
        {
            if (_isLoadRunning)
            {
                Debug.LogWarning("[GameEntry] Load request ignored, another load is already running.");
                return LoadResult.Cancelled;
            }

            _isLoadRunning = true;
            
            // Set up a linked CTS so we can cancel it
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _cts = linked;

            var sceneListBeforeLoad = string.Join(", ",
                Enumerable.Range(0, SceneManager.sceneCount).Select(i => SceneManager.GetSceneAt(i).name));
            Debug.Log($"[GameEntry] Scene list before load: {sceneListBeforeLoad}");

            try
            {
                SetLoading(true);

                // Ensure MainScene is loaded and managers are alive
                var mainScene = SceneHandler.GetSceneByName(MAIN_SCENE);
                if (!mainScene.isLoaded)
                {
                    var lr = await LoadMainSceneAsync(mainSceneAdditive, linked.Token);
                    if (lr != LoadResult.Ok)
                    {
                        return Fail(lr, "Failed to load MainScene.");
                    }
                }
                
                Debug.Log("[GameEntry] Scene list after main scene load: " +
                          string.Join(", ", Enumerable.Range(0, SceneManager.sceneCount)
                              .Select(i => SceneManager.GetSceneAt(i).name)));

                // Wait until critical managers are ready
                var readyManagers = await EnsureManagersReadyAsync(linked.Token);
                if (!readyManagers)
                {
                    return Fail(LoadResult.ManagersNotFound, "Managers not ready.");
                }

                // Only after mangers are ready, notify about preload 
                NotifyPreload(level);

                // Bind session & Load
                var sessionRes = await TryAwaitBindSession(session, linked.Token);
                if (sessionRes != LoadResult.Ok)
                {
                    return Fail(sessionRes, "Failed to bind session.");
                }

                
                // Unload old gameplay scenes first
                await UnloadTrackedScenesAsync(linked.Token);
                
                // Load the new gameplay scenes
                var targetLevel = GetLevelToLoad(session);
                var loadRes = await LoadLevelScenesAsync(targetLevel, linked.Token);
                if (loadRes != LoadResult.Ok)
                {
                    return Fail(loadRes, "Failed to load level scenes.");
                }

                var managerValue = await GlobalLevelManager.GetAsync(linked.Token);
                if (!managerValue.TryGet(out var manager))
                {
                    return Fail(LoadResult.ManagersNotFound, "Level Manager not found after scene load.");
                }

                await targetLevel.OnScenesLoadedAsync(manager, linked.Token);
                
                // Notify loaded
                NotifyLoaded(targetLevel);

                // Respawn the player
                //linked.Token.ThrowIfCancellationRequested();
                await PlayerSpawner.RespawnPlayerAsync(linked.Token);

                SetLoading(false);
                NotifyReady(targetLevel);
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
                _isLoadRunning = false;
            }
            
            LoadResult Fail(LoadResult code, string msg)
            {
                LoadingState.SetLoading(false);
                ForceReturnToMainMenu(code, msg);
                return code;
            }
        }

        #region Try Catch tests
        
        private static BaseLevelSO GetLevelToLoad(GameSessionSO session)
        {
            if (session == null || session.Level == null)
                return null;

            var rootLevel = session.Level;
            var index = Mathf.Max(0, session.CurrIndex);

            if (rootLevel.LevelCount() <= 1)
                return rootLevel;

            var childLevel = rootLevel.GetLevel(index);
            return childLevel ? childLevel : rootLevel;
        }

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
        
        private static async UniTask UnloadTrackedScenesAsync(CancellationToken ct)
        {
            for (var i = _loadedScenes.Count - 1; i >= 0; i--)
            {
                ct.ThrowIfCancellationRequested();

                var scene = _loadedScenes[i];
                if (!scene.IsValid() || !scene.isLoaded)
                    continue;

                var unloadOp = SceneHandler.UnloadSceneAsync(scene);
                if (unloadOp != null)
                {
                    await unloadOp.ToUniTask(cancellationToken: ct);
                }
            }

            _loadedScenes.Clear();
        }

        private static async UniTask<LoadResult> LoadLevelScenesAsync(BaseLevelSO level, CancellationToken ct)
        {
            if (level.IsNullOrMissing())
            {
                return LoadResult.MissingLevel;
            }

            var requests = level.GetSceneLoadRequests();
            if (requests == null || requests.Count == 0)
            {
                return LoadResult.MissingLevel;
            }

            Scene activeScene = default;
            var hasActiveScene = false;

            for (var i = 0; i < requests.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var request = requests[i];
                if (string.IsNullOrWhiteSpace(request.SceneName))
                {
                    return LoadResult.MissingLevel;
                }

                var op = SceneHandler.LoadSceneAsync(request.SceneName, request.LoadMode);
                if (op == null)
                {
                    return LoadResult.SceneLoadFailed;
                }

                await op.ToUniTask(cancellationToken: ct);

                var loadedScene = SceneHandler.GetSceneByName(request.SceneName);
                if (!loadedScene.IsValid() || !loadedScene.isLoaded)
                {
                    return LoadResult.SceneLoadFailed;
                }
                
                _loadedScenes.Add(loadedScene);

                if (request.SetActive && !hasActiveScene)
                {
                    activeScene = loadedScene;
                    hasActiveScene = true;
                }
            }

            if (hasActiveScene)
            {
                SceneManager.SetActiveScene(activeScene);
            }
            else if (_loadedScenes.Count > 0)
            {
                SceneManager.SetActiveScene(_loadedScenes[0]);
            }

            return LoadResult.Ok;
        }

        private static async UniTask<LoadResult> TryAwaitBindSession(GameSessionSO session, CancellationToken ct)
        {
            var managerValue = await GlobalLevelManager.GetAsync(ct);
            if (!managerValue.TryGet(out var manager))
            {
                return LoadResult.ManagersNotFound;
            }
            
            // Tell the manager which level to load
            ct.ThrowIfCancellationRequested();
            manager.SetSession(session);
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

        private static async void ForceReturnToMainMenu(LoadResult code, string reason)
        {
            Debug.LogError($"[GameEntry] Load fail ({(int)code}): {reason}");
            LoadingState.DetachAll();
            
            // Cancel anything still running
            try
            {
                _cts?.Cancel();
            }
            catch
            {
                // Ignore
            }
            
            // Ensure we’re on main thread
            await UniTask.SwitchToMainThread();
            
            // Close loading UI
            try
            {
                LoadingState.SetLoading(false);
            }
            catch
            {
                // Ignore
            }
            
            // Unload any additive gameplay scenes
            try
            {
                // If you have a central place that knows loaded scenes, use that.
                // Otherwise, unload everything except the menu we’re about to load.
                // (If your SceneHandler can list LoadedScenes, iterate and unload.)
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GameEntry] Unload before menu: {ex}");
            }

            try
            {
                await UnloadTrackedScenesAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GameEntry] Could not unload tracked gameplay scenes: {ex}");
            }
            
            // Load menu, then set it active
            try
            {
                SceneHandler.LoadScene(SceneHandler.MainMenuName);
                var menuScene = SceneHandler.GetSceneByName(SceneHandler.MainMenuName);
                if (menuScene.IsValid() && menuScene.isLoaded)
                    SceneManager.SetActiveScene(menuScene);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameEntry] Failed to load main menu: {ex}");
                return;
            }
            
            // ToDo: show pop up.
#if UNITY_EDITOR
            UnityEditor.EditorUtility.DisplayDialog($"Loading Error {code}", reason, "OK");
#endif
        }
    }

    public class LoadingState
    {
        public bool Loading { get; private set; }
        public ISubject LevelChanged => _onLevelReady.ConvertToSimple();
        
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
        public bool AttachOnLoaded(DesignPatterns.Observers.IObserver<BaseLevelSO> observer, bool disposeOnDetach = false)
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