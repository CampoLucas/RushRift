using UnityEditor;
using UnityEngine;

public static class SaveFileEditor
{
    [MenuItem("Tools/Save Data/Open Save File Location")]
    private static void OpenSaveFolder() => EditorUtility.RevealInFinder(SaveSystem.SaveFilePath);

    [MenuItem("Tools/Save Data/Copy Save File Path")]
    private static void CopySavePath()
    {
        EditorGUIUtility.systemCopyBuffer = SaveSystem.SaveFilePath;
        Debug.Log($"Copied Save File Path: {SaveSystem.SaveFilePath}");
    }

    [MenuItem("Tools/Save Data/New Save File")]
    private static void ResetSaveFile()
    {
        SaveSystem.ResetGame();
        Debug.Log("Save Data has been set to new.");
    }
    
    
    [MenuItem("Tools/Save Data/New Settings File")]
    private static void ResetSettingsSaveFile()
    {
        SaveSystem.ResetSettings();
        Debug.Log("Save Settings Data has been set to new.");
    }
    
}