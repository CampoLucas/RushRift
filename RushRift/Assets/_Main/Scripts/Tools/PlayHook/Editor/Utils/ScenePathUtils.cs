using UnityEditor;
using UnityEngine;

namespace Tools.PlayHook.Utils
{
    public static class ScenePathUtils
    {
        public static bool TryGetScenePathByName(string sceneName, out string path)
        {
            return TryGetScenePathFromBuildSettings(sceneName, out path)
                   || TryGetScenePathFromAssets(sceneName, out path);
        }

        private static bool TryGetScenePathFromBuildSettings(string sceneName, out string path)
        {
            path = null;
            
            foreach (var scene in EditorBuildSettings.scenes)
            {
                var p = scene.path;
                var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                if (fileName == sceneName)
                {
                    path = p;
                    return true;
                }
            }

            return false;
        }
        
        private static bool TryGetScenePathFromAssets(string sceneName, out string path)
        {
            path = null;
            
            var guids = AssetDatabase.FindAssets($"{sceneName} t:Scene");

            foreach (var guid in guids)
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                if (fileName == sceneName)
                {
                    path = p;
                    return true;
                }
            }

            return false;
        }
    }
}