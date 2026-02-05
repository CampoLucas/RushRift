using System.Collections;
using System.Collections.Generic;
using MyTools.Global;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FixScalesTool
{
    private const string Tag = "[FixNegativeScale]";

    [MenuItem("Tools/Scene Management/Fix Collider's Negative Scale")]
    private static void Fix()
    {
        FixNegativeScales(true);
    }

    private static void FixNegativeScales(bool includeInactive)
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.isLoaded)
        {
            LogWarning($"No active scene loaded.");
            return;
        }
        
        var changedCount = 0;

        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var col in root.GetComponentsInChildren<Collider>(true))
            {
                var tr = col.transform;
                var oldScale = tr.localScale;
                
                if (oldScale.x >= 0f && oldScale.y >= 0f && oldScale.z >= 0f)
                    continue;

                var newScale = new Vector3(Mathf.Abs(oldScale.x), Mathf.Abs(oldScale.y), Mathf.Abs(oldScale.z));
                
                Undo.RecordObject(tr, "Fix Negative Scale (Collider Object)");
                tr.localScale = newScale;
                EditorUtility.SetDirty(tr);

                changedCount++;
                Log($"Changed the scale of {tr.gameObject.name}, from {oldScale} to {newScale}. Path: {GetFullPath(tr)}", tr.gameObject);
            }
        }
        
        Log($"Done. Fixed the scale of {changedCount} objects in the scene {scene.name}");
    }
    
    private static string GetFullPath(Transform t)
    {
        var path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = $"{t.name}/{path}";
        }
        return path;
    }

    private static void Log(string message, Object context = null)
    {
        Debug.Log($"{Tag} {message}", context);
    }

    private static void LogWarning(string message, Object context = null)
    {
        Debug.LogWarning($"{Tag} {message}", context);
    }
    
    private static void LogError(string message, Object context = null)
    {
        Debug.LogError($"{Tag} {message}", context);
    }
}
