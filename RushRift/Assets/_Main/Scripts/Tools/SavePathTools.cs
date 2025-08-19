#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

static class SavePathTools
{
    [MenuItem("Tools/Save Data/Open Save File Location")]
    static void OpenSaveFolder() => EditorUtility.RevealInFinder(SaveAndLoad.SaveFilePath);

    [MenuItem("Tools/Save Data/Copy Save File Path")]
    static void CopySavePath()
    {
        EditorGUIUtility.systemCopyBuffer = SaveAndLoad.SaveFilePath;
        Debug.Log($"Copied: {SaveAndLoad.SaveFilePath}");
    }
}
#endif