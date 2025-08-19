using UnityEditor;
using UnityEngine;

public static class SaveFileEditor
{
    [MenuItem("Tools/Save Data/Open Save File Location")]
    private static void OpenSaveFolder() => EditorUtility.RevealInFinder(SaveAndLoad.SaveFilePath);

    [MenuItem("Tools/Save Data/Copy Save File Path")]
    private static void CopySavePath()
    {
        EditorGUIUtility.systemCopyBuffer = SaveAndLoad.SaveFilePath;
        Debug.Log($"Copied Save File Path: {SaveAndLoad.SaveFilePath}");
    }

    [MenuItem("Tools/Save Data/New Save File")]
    private static void ResetSaveFile()
    {
        SaveAndLoad.Reset();
        Debug.Log("Save Data has been set to new.");
    }
    
    [MenuItem("Tools/Save Data/Add 1000 Currency")]
    private static void AddMoney()
    {
        SaveAndLoad.AddMoney(1000);
        Debug.Log("Added 1000 to currency.");
    }
    
    
}