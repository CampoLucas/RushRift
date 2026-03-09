using UnityEngine.SceneManagement;

namespace Game
{
    public readonly struct LevelSceneRequest
    {
        public readonly string SceneName;
        public readonly LoadSceneMode LoadMode;
        public readonly bool SetActive;

        public LevelSceneRequest(string sceneName, LoadSceneMode loadMode = LoadSceneMode.Additive, bool setActive = false)
        {
            SceneName = sceneName;
            LoadMode = loadMode;
            SetActive = setActive;
        }
    }
}