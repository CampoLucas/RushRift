using System;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BehaviourTreeAsset.Utils
{
    public static class DebugUtils
    {
        public static void LogNullReference(object obj)
        {
            if (obj != null) return;
            // Get the calling method's information
            var stackTrace = new StackTrace();
            var callingFrame = stackTrace.GetFrame(1); // Skip the current method
            var callingMethod = callingFrame.GetMethod();
            var methodName = callingMethod.Name;

            // Get the declaring type (class) information
            var declaringType = callingMethod.DeclaringType;
            var className = declaringType != null ? declaringType.Name : "UnknownClass";

            // Log the null reference
            Debug.LogError($"Null reference detected in {className}.{methodName}");
        }
    }
}