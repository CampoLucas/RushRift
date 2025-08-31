using System;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Game.General.Utils
{
    public static class FileLogger
    {
        public static readonly string LogFolderPath =
            Path.Combine(Application.persistentDataPath, "logs", Application.version);
        
        private static string _logFilePath;
        private static bool _initialized = false;

#if UNITY_EDITOR
        [MenuItem("Tools/File Logger/Open Log Folder Location")]
        private static void OpenLogsFolder()
        {
            // Make sure the folder exists
            Directory.CreateDirectory(LogFolderPath);
            
            EditorUtility.RevealInFinder(LogFolderPath + "/");
        }
#endif
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void InitAfterAssemblies()
        {
            InitializeLogger(true);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitBeforeScene()
        {
            InitializeLogger(false);
        }
        

        private static void InitializeLogger(bool afterAssembliesLoaded)
        {
            if (_initialized) return; // only initialize once
            _initialized = true;
            
            // Build folder structure: persistentDataPath/logs/<version>/
            var logFolder = Path.Combine(Application.persistentDataPath, "logs", Application.version);
            
            // Make sure the folder exists
            Directory.CreateDirectory(logFolder);
            
            // Create readable timestamp: yyyy-MM-dd_HH-mm-ss
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            
            // Full path: persistentDataPath/logs/<version>/<timestamp>.txt
            _logFilePath = Path.Combine(logFolder, $"{timestamp}.txt");
            
            // Start file
            var afterAssemblies = "AfterAssembliesLoaded";
            var beforeSceneLoaded = "BeforeSceneLoaded";
            File.WriteAllText(_logFilePath, $"=== Game Log Started ({DateTime.Now}) (Moment: {(afterAssembliesLoaded ? afterAssemblies : beforeSceneLoaded)}) ===\n");

            // Subscribe to Unity log events
            Application.logMessageReceived += HandleLog;
            
            // Unsubscribe when the app is closing
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        }
        
        private static void HandleLog(string logString, string stackTrace, LogType type)
        {
            var entry = $"[{type}] {logString}\n";
            if (type == LogType.Error || type == LogType.Exception)
                entry += stackTrace + "\n";

            File.AppendAllText(_logFilePath, entry);
        }
        
        private static void OnProcessExit(object sender, EventArgs e)
        {
            Application.logMessageReceived -= HandleLog;
            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
        }
    }
}