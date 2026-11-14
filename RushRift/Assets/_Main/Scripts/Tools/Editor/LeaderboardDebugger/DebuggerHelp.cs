using UnityEditor;

namespace Game.LeaderboardDebugger
{
    /// <summary>
    /// HelpBox helpers. Info boxes obey the Show help setting, warnings and errors are always shown.
    /// </summary>
    public static class DebuggerHelp
    {
        public static void Info(DebuggerContext context, string text)
        {
            if (context.ShowHelp) EditorGUILayout.HelpBox(text, MessageType.Info);
        }

        public static void Warning(string text)
        {
            EditorGUILayout.HelpBox(text, MessageType.Warning);
        }

        public static void Error(string text)
        {
            EditorGUILayout.HelpBox(text, MessageType.Error);
        }
    }
}
