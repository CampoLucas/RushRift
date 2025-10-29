using System;
using UnityEditor;

namespace Tools.PlayHook
{
    public static class PlayLevelSelectionBridge
    {
        public static event Action OnSelectionChanged;

        public static void NotifyChanged()
        {
            OnSelectionChanged?.Invoke();
        }

        public static string GetLevelPath() => EditorPrefs.GetString("PlayLevel.SelectedLevel", "");
        public static string GetModePath()  => EditorPrefs.GetString("PlayLevel.SelectedMode", "");
    }
}