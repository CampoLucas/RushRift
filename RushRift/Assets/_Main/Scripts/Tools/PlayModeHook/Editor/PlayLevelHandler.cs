using UnityEditor;
using Cysharp.Threading.Tasks;
using Game;
using Game.Levels;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Handles running the selected BaseLevelSO when entering PlayMode.</summary>
[InitializeOnLoad]
public static class PlayLevelHandler
{
    private const string PrefKey = "PlayLevel.SelectedLevel";
    private static BaseLevelSO _selected;

    static PlayLevelHandler()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static async void OnPlayModeChanged(UnityEditor.PlayModeStateChange state)
    {
        if (state != UnityEditor.PlayModeStateChange.EnteredPlayMode) return;

        // Reset if disabled
        var path = EditorPrefs.GetString(PrefKey, "");
        if (string.IsNullOrEmpty(path) || path == PlayLevelToolbar.DisabledFlag)
        {
            // Ensure playModeStartScene is cleared
            EditorSceneManager.playModeStartScene = null;
            Debug.Log($"[{typeof(PlayLevelHandler)}] Tool disabled. Skipping auto load.");
            return;
        }

        // Try load level asset
        _selected = AssetDatabase.LoadAssetAtPath<BaseLevelSO>(path);
        if (_selected == null)
        {
            // Ensure playModeStartScene is cleared
            EditorSceneManager.playModeStartScene = null;
            Debug.Log($"[{typeof(PlayLevelHandler)}] Selected level asset missing or invalid — skipping.");
            return;
        }

        await UniTask.DelayFrame(5); // let managers init
        GameEntry.LoadLevelAsync(_selected, false);
    }

    public static void SetSelectedLevel(BaseLevelSO level)
    {
        _selected = level;

        if (level == null)
        {
            // Disable the feature entirely
            EditorPrefs.SetString(PrefKey, PlayLevelToolbar.DisabledFlag);
            Debug.Log($"[{typeof(PlayLevelHandler)}] Level selection cleared — tool disabled.");
            return;
        }
        
        var path = AssetDatabase.GetAssetPath(level);
        EditorPrefs.SetString(PrefKey, path);
    }
}