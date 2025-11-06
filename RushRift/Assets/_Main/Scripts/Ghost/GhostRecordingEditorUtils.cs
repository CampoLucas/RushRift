#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Main.Scripts.Ghost
{
    public static class GhostRecordingEditorUtils
    {
        private const string PrefKeyLogs = "GhostRecordingEditor_EnableLogs";

        public static bool EditorLogsEnabled
        {
            get => EditorPrefs.GetBool(PrefKeyLogs, false);
            set => EditorPrefs.SetBool(PrefKeyLogs, value);
        }

        public static string ResolveGhostFolder(SerializedObject so)
        {
            var folderProp = so.FindProperty("ghostsFolderName");
            string folderName = folderProp != null ? folderProp.stringValue : "ghosts";
            return Path.Combine(Application.persistentDataPath, folderName);
        }

        public static string ResolveGhostFileForCurrentScene(SerializedObject so)
        {
            int level = SceneManager.GetActiveScene().buildIndex;
            var patternProp = so.FindProperty("fileNamePattern");
            string pattern = patternProp != null ? patternProp.stringValue : "level_{LEVEL}.ghost.json";
            string file = pattern.Replace("{LEVEL}", level.ToString());
            return Path.Combine(ResolveGhostFolder(so), file);
        }

        public static void OpenGhostFolder(SerializedObject so)
        {
            string path = ResolveGhostFolder(so);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            if (EditorLogsEnabled) Debug.Log($"[GhostTools] Open folder: {path}");
            EditorUtility.RevealInFinder(path);
        }

        public static void DeleteCurrentSceneGhost(SerializedObject so)
        {
            string file = ResolveGhostFileForCurrentScene(so);
            if (!File.Exists(file))
            {
                EditorUtility.DisplayDialog("Delete Ghost", "No ghost file found for the current scene.", "OK");
                if (EditorLogsEnabled) Debug.Log($"[GhostTools] Delete skipped (not found): {file}");
                return;
            }

            if (!EditorUtility.DisplayDialog("Delete Ghost", $"Delete ghost file?\n\n{file}", "Delete", "Cancel"))
                return;

            File.Delete(file);
            if (EditorLogsEnabled) Debug.Log($"[GhostTools] Deleted: {file}");
        }
    }

    [CustomEditor(typeof(GhostRecorder))]
    public class GhostRecorderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Ghost Utilities", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("Open Ghost Folder", "Open the folder that stores ghost recordings")))
                    GhostRecordingEditorUtils.OpenGhostFolder(serializedObject);

                if (GUILayout.Button(new GUIContent("Delete Current Scene Ghost", "Delete the ghost file recorded for the active scene")))
                    GhostRecordingEditorUtils.DeleteCurrentSceneGhost(serializedObject);
            }

            GhostRecordingEditorUtils.EditorLogsEnabled = EditorGUILayout.Toggle(
                new GUIContent("Editor Debug Logs", "Print editor logs for ghost utilities"),
                GhostRecordingEditorUtils.EditorLogsEnabled
            );
        }
    }

    [CustomEditor(typeof(GhostPlayer))]
    public class GhostPlayerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Ghost Utilities", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("Open Ghost Folder", "Open the folder that stores ghost recordings")))
                {
                    var rec = FindObjectOfType<GhostRecorder>();
                    if (rec) GhostRecordingEditorUtils.OpenGhostFolder(new SerializedObject(rec));
                    else EditorUtility.DisplayDialog("Open Ghost Folder", "No GhostRecorder found in the scene to resolve paths.", "OK");
                }

                if (GUILayout.Button(new GUIContent("Delete Current Scene Ghost", "Delete the ghost file recorded for the active scene")))
                {
                    var rec = FindObjectOfType<GhostRecorder>();
                    if (rec) GhostRecordingEditorUtils.DeleteCurrentSceneGhost(new SerializedObject(rec));
                    else EditorUtility.DisplayDialog("Delete Ghost", "No GhostRecorder found in the scene to resolve paths.", "OK");
                }
            }

            GhostRecordingEditorUtils.EditorLogsEnabled = EditorGUILayout.Toggle(
                new GUIContent("Editor Debug Logs", "Print editor logs for ghost utilities"),
                GhostRecordingEditorUtils.EditorLogsEnabled
            );
        }
    }
}
#endif