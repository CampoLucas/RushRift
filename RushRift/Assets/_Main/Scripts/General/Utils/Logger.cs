using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MyTools.Global
{
    public static class Logger
    {
        [Conditional("UNITY_EDITOR")]
        public static void Log(this object obj, string message, Object context = null, LogType logType = LogType.Log)
        {
            switch (logType)
            {
                case LogType.Warning:
                    Debug.LogWarning($"[{obj.GetType().Name}] WARNING: {message}", context);
                    break;
                case LogType.Error:
                    Debug.LogError($"[{obj.GetType().Name}] ERROR: {message}", context);
                    break;
                default:
                    Debug.Log($"[{obj.GetType().Name}] {message}", context);
                    break;
            }
        }
        
        [Conditional("UNITY_EDITOR")]
        public static void Log(this Object obj, string message, LogType logType = LogType.Log)
        {
            Log(obj, message, obj, logType);
        }
    }
}