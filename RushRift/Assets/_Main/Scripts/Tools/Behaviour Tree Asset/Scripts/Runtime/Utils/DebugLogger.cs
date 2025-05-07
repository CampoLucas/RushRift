using System;
using UnityEngine;

namespace BehaviourTreeAsset.Utils
{
    public static class DebugLogger
    {
        private const string LogConditional = "DEBUG_LOGGER_ENABLED";
        
        [System.Diagnostics.Conditional(LogConditional)]
        public static void Log(string message, GameObject context = null, LogType type = LogType.Log, string tag = "")
        {
            switch (type)
            {
                case LogType.Log:
                    Debug.Log($"Log: [{tag}] {message}", context);
                    break;
                case LogType.Warning:
                    Debug.LogWarning($"Warning: [{tag}] {message}", context);
                    break;
                case LogType.Error:
                    Debug.LogError($"Error: [{tag}] {message}", context);
                    break;
                default:
                    break;
            }
        }
        
        [System.Diagnostics.Conditional(LogConditional)]
        public static void EditorLog(string message, GameObject context = null, LogType type = LogType.Log, string tag = "")
        {
#if UNITY_EDITOR
            Log(message, context, type, tag);
#endif
        }

        public enum LogType
        {
            Log, Warning, Error
        }
    }
}