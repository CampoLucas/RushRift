using UnityEditor;

namespace Game.GhostDebugger
{
    /// <summary>
    /// HelpBox helpers. Info boxes obey the Show help setting, warnings and errors are always shown.
    /// </summary>
    public static class GhostDebuggerHelp
    {
        public static void Info(GhostDebuggerContext context, string text)
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
