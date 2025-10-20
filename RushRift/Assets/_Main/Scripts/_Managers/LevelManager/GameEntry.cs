using Cysharp.Threading.Tasks;
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

        public static async UniTask OnMainSceneLoaded(GlobalLevelManager manager)
        {
            if (PendingLevel == null)
                return;

            await PendingLevel.LoadAsync(manager);
            PendingLevel = null;
        }
        
        public static async void LoadLevelAsync(BaseLevelSO level)
        {
            var mainScene = SceneHandler.GetSceneByName(MAIN_SCENE);
            
            // If the main scene is not loaded, load it additively.
            if (!mainScene.isLoaded)
            {
                SceneHandler.LoadScene(MAIN_SCENE);
            }
            
            // Find the level manager in the MainScene
            var managerCheck = await GlobalLevelManager.GetAsync();
            if (!managerCheck.TryGet(out var manager))
            {
                Debug.LogError($"{typeof(GlobalLevelManager).Name} was not found.");
                return;
            }

            // Tell the manager which level to load
            await manager.LoadLevelAsync(level);
            
            // Unload the previous scene (Hub, menu, etc.)
            var active = SceneHandler.GetActiveScene();
            if (active.name != MAIN_SCENE)
            {
                var unloadPrev = SceneHandler.UnloadSceneAsync(active);
                await unloadPrev.ToUniTask();
            }
        }
        
        public static async void TryLoadLevelAsync(BaseLevelSO level)
        {
            var mainScene = SceneHandler.GetSceneByName(MAIN_SCENE);
            
            // If the main scene is not loaded, load it additively.
            if (!mainScene.isLoaded)
            {
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
            
            // Find the level manager in the MainScene
            var managerCheck = await GlobalLevelManager.GetAsync();
            if (!managerCheck.TryGet(out var manager))
            {
                Debug.LogError($"{typeof(GlobalLevelManager).Name} was not found.");
                return;
            }

            // Tell the manager which level to load
            await manager.LoadLevelAsync(level);
            
            // Unload the previous scene (Hub, menu, etc.)
            var active = SceneHandler.GetActiveScene();
            if (active.name != MAIN_SCENE)
            {
                var unloadPrev = SceneHandler.UnloadSceneAsync(active);
                await unloadPrev.ToUniTask();
            }
        }
    }
}