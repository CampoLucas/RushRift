using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MyTools.Global
{
    public static class Logger
    {
        public enum LogType
        {
            Log,
            Warning,
            Error
        }
        
        [Conditional("UNITY_EDITOR")]
        public static void Log(this object obj, string message, Object context = null, LogType logType = LogType.Log)
        {
            switch (logType)
            {
                case LogType.Warning:
                    Debug.LogWarning($"WARNING: {message}", context);
                    break;
                case LogType.Error:
                    Debug.LogError($"ERROR: {message}", context);
                    break;
                default:
                    Debug.Log(message, context);
                    break;
            }
        }
    }
}