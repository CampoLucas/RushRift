using System.Threading;
using Game.DesignPatterns.Observers;
using Game.Saves;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Utils
{
    public static class SceneHandler
    {
        public static readonly string MainMenuName = "Main Menu";
        public static readonly string HubName = "HUB";
        public static readonly string FirstLevel = "Level01";
        public static readonly int MainMenuIndex = SceneUtility.GetBuildIndexByScenePath(MainMenuName);
        public static readonly int HubIndex = SceneUtility.GetBuildIndexByScenePath(HubName);
        public static readonly int FirstLevelIndex = SceneUtility.GetBuildIndexByScenePath(FirstLevel);
        
        /// <summary>
        /// OnSceneChangedIndex(from, to, isAsync)
        /// </summary>
        public static ISubject<int, int, bool> OnSceneChangedIndex { get; } = new Subject<int, int, bool>();
        
        /// <summary>
        /// OnSceneChangedName(from, to, isAsync)
        /// </summary>
        public static ISubject<string, string, bool> OnSceneChangedName { get; } = new Subject<string, string, bool>();
        public static ISubject OnSceneChanged { get; } = new Subject();

        #region General Use

        public static void LoadScene(int index)
        {
            var current = SceneManager.GetActiveScene();
            var name = GetSceneNameByIndex(index);
            
            SaveCurrentScene(current, name, index);
            NotifySceneChange(current, name, index, false);
            SceneManager.LoadScene(index);
        }

        public static void LoadScene(string name)
        {
            var current = SceneManager.GetActiveScene();
            var index = GetSceneIndexByName(name);
            
            SaveCurrentScene(current, name, index);
            NotifySceneChange(current, name, index, false);
            SceneManager.LoadScene(name);
        }
        
        public static AsyncOperation LoadSceneAsync(string name, LoadSceneMode loadSceneMode = LoadSceneMode.Single)
        {
            var current = SceneManager.GetActiveScene();
            var index = GetSceneIndexByName(name);
            
            SaveCurrentScene(current, name, index);
            NotifySceneChange(current, name, index, true);
            return SceneManager.LoadSceneAsync(name, loadSceneMode);
        }
        
        public static AsyncOperation LoadSceneAsync(int index)
        {
            var current = SceneManager.GetActiveScene();
            var name = GetSceneNameByIndex(index);
            
            SaveCurrentScene(current, name, index);
            NotifySceneChange(current, name, index, true);
            return SceneManager.LoadSceneAsync(index);
        }

        public static string GetCurrentSceneName()
        {
            return SceneManager.GetActiveScene().name;
        }

        public static int GetCurrentSceneIndex()
        {
            return SceneManager.GetActiveScene().buildIndex;
        }

        public static int GetSceneCount()
        {
            return SceneManager.sceneCountInBuildSettings;
        }

        public static void ReloadCurrent()
        {
            var current = SceneManager.GetActiveScene().name;
            LoadScene(current);
        }
        
        public static Scene GetActiveScene()
        {
            return SceneManager.GetActiveScene();
        }

        #endregion

        #region Load Specific scenes

        public static void LoadMainMenu()
        {
            LoadSceneAsync(MainMenuName);
        }

        public static void LoadHub()
        {
            LoadSceneAsync(HubName);
        }
        
        public static void LoadFirstLevel()
        {
            LoadSceneAsync(FirstLevel);
        }
        
        public static void LoadLastLevel()
        {
            var data = SaveSystem.LoadGame();

            LoadSceneAsync(data.lastSceneIndex);
        }
        
        #endregion
        
        private static void SaveCurrentScene(Scene from, string nextSceneName, int nextSceneIndex)
        {
#if UNITY_EDITOR
            Debug.Log($"[SceneHandler] Saving Current Scene from: {from.name} to: {nextSceneName}");
#endif
            var fromName = from.name;

            var sceneToSave = "";
            var sceneToSaveIndex = 0;
            
            if (IsSaveable(nextSceneName))
            {
                sceneToSave = nextSceneName;
                sceneToSaveIndex = nextSceneIndex;
            }
            else if (IsSaveable(fromName))
            {
                sceneToSave = fromName;
                sceneToSaveIndex = from.buildIndex;
            }
            else
            {
                return;
            }
            
            var save = SaveSystem.LoadGame();
            save.SetLastScene(sceneToSave, sceneToSaveIndex);
            save.SaveGame();
        }

        private static bool IsSaveable(string sceneName)
        {
            return sceneName != MainMenuName && sceneName != GameEntry.MAIN_SCENE;
        }

        private static void NotifySceneChange(Scene from, string toName, int toIndex, bool isAsync)
        {
            OnSceneChanged.NotifyAll();
            OnSceneChangedName.NotifyAll(from.name, toName, isAsync);
            OnSceneChangedIndex.NotifyAll(from.buildIndex, toIndex, isAsync);
        }
        
        private static string GetSceneNameByIndex(int index)
        {
            var path = SceneUtility.GetScenePathByBuildIndex(index); // e.g. "Assets/Scenes/Level01.unity"
            var fileName = System.IO.Path.GetFileNameWithoutExtension(path); // "Level01"
            return fileName;
        }

        private static int GetSceneIndexByName(string name)
        {
            return SceneUtility.GetBuildIndexByScenePath(name);
        }

        public static Scene GetSceneByName(string name)
        {
            return SceneManager.GetSceneByName(name);
        }

        public static AsyncOperation UnloadSceneAsync(Scene active)
        {
            return SceneManager.UnloadSceneAsync(active);
        }

        public static void SetActiveScene(Scene scene)
        {
            SceneManager.SetActiveScene(scene);
        }
    }
}