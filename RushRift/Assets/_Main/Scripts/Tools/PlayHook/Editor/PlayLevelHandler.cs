using UnityEditor;
using Cysharp.Threading.Tasks;
using Game;
using Game.Levels;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tools.PlayHook
{
     /// <summary>Handles running the selected BaseLevelSO when entering PlayMode.</summary>
        [InitializeOnLoad]

    public static class PlayLevelHandler
    {
        private const string LevelPrefKey = "PlayLevel.SelectedLevel";
        private const string ModePrefKey = "PlayLevel.SelectedMode";
        private static BaseLevelSO _selectedLevel;
        private static GameModeSO _selectedMode;

        static PlayLevelHandler()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static async void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;

            // Level
            // Reset if disabled
            var levelPath = EditorPrefs.GetString(LevelPrefKey, "");
            if (string.IsNullOrEmpty(levelPath) || levelPath == PlayLevelToolbar.DisabledFlag)
            {
                // Ensure playModeStartScene is cleared
                EditorSceneManager.playModeStartScene = null;
                Debug.Log($"[{typeof(PlayLevelHandler)}] Tool disabled. Skipping auto load.");
                return;
            }

            // Try load level asset
            _selectedLevel = AssetDatabase.LoadAssetAtPath<BaseLevelSO>(levelPath);
            if (_selectedLevel == null)
            {
                // Ensure playModeStartScene is cleared
                EditorSceneManager.playModeStartScene = null;
                Debug.Log($"[{typeof(PlayLevelHandler)}] Selected level asset missing or invalid — skipping.");
                return;
            }

            // Game Mode
            var modePath = EditorPrefs.GetString(ModePrefKey, "");
            if (!string.IsNullOrEmpty(modePath) && modePath != PlayLevelToolbar.DisabledFlag)
            {
                _selectedMode = AssetDatabase.LoadAssetAtPath<GameModeSO>(modePath);
            }
            else
            {
                _selectedMode = null;
            }


            await UniTask.DelayFrame(5); // let managers init
            if (!_selectedMode)
            {
                GameEntry.LoadLevelAsync(_selectedLevel, false);
            }
            else
            {
                GameEntry.LoadSessionAsync(_selectedMode, _selectedLevel, false);
            }
        }

        public static void SetSelectedLevel(BaseLevelSO level)
        {
            _selectedLevel = level;

            if (level == null)
            {
                // Disable the feature entirely
                EditorPrefs.SetString(LevelPrefKey, PlayLevelToolbar.DisabledFlag);
                Debug.Log($"[{typeof(PlayLevelHandler)}] Level selection cleared — tool disabled.");
                return;
            }

            var path = AssetDatabase.GetAssetPath(level);
            EditorPrefs.SetString(LevelPrefKey, path);
        }

        public static void SetSelectedMode(GameModeSO mode)
        {
            _selectedMode = mode;

            if (mode == null)
            {
                // Disable the feature entirely
                EditorPrefs.SetString(ModePrefKey, PlayLevelToolbar.DisabledFlag);
                Debug.Log($"[{typeof(PlayLevelHandler)}] Mode selection cleared — tool disabled.");
                return;
            }

            var path = AssetDatabase.GetAssetPath(mode);
            EditorPrefs.SetString(ModePrefKey, path);
        }
    }
}