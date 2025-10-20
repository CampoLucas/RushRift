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
        public static BaseLevelSO PendingLevel;
        public const string MAIN_SCENE = "MainScene";

        #region Events

        public static Subject<BaseLevelSO> LoadingLevelStart = new();
        public static Subject<BaseLevelSO> LoadingLevelEnd = new();
        public static Subject<BaseLevelSO> ResettingLevelStart = new();
        public static Subject<BaseLevelSO> ResettingLevelEnd = new();

        #endregion

        public static async UniTask OnMainSceneLoaded(GlobalLevelManager manager)
        {
            if (PendingLevel == null)
                return;

            await PendingLevel.LoadAsync(manager);
            PendingLevel = null;
        }
        
        public static async void TryLoadLevelAsync(BaseLevelSO level, bool mainSceneAdditive = false)
        {
            StartLoadingLevel(level);
            
            var mainScene = SceneHandler.GetSceneByName(MAIN_SCENE);

            if (!mainScene.isLoaded)
            {
                await LoadMainSceneAsync(mainSceneAdditive);
            }
            
            await UnloadPrevSceneAsync();
            await LoadLevelAsync(level);
            StopLoadingLevel(level);
        }


        private static void StartLoadingLevel(BaseLevelSO level)
        {
            LoadingLevelStart.NotifyAll(level);
            PauseHandler.Pause(true);
            GlobalLevelManager.LoadingLevel = true;
        }

        private static void StopLoadingLevel(BaseLevelSO level)
        {
            GlobalLevelManager.LoadingLevel = false;
            PauseHandler.Pause(false);
            LoadingLevelEnd.NotifyAll(level);
        }

        private static async UniTask UnloadPrevSceneAsync()
        {
            // Unload the previous scene (Hub, menu, etc.)
            var active = SceneHandler.GetActiveScene();
            if (active.name != MAIN_SCENE)
            {
                var unloadPrev = SceneHandler.UnloadSceneAsync(active);
                await unloadPrev.ToUniTask();
            }
        }

        private static async UniTask LoadLevelAsync(BaseLevelSO level)
        {
            // Find the level manager in the MainScene
            var managerCheck = await GlobalLevelManager.GetAsync();
            if (!managerCheck.TryGet(out var manager))
            {
                Debug.LogError($"{typeof(GlobalLevelManager).Name} was not found.");
                return;
            }
            
            // Tell the manager which level to load
            await manager.LoadLevelAsync(level);
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
}