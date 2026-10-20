using UnityEditor;
using UnityEngine;

namespace Game.LeaderboardDebugger
{
    /// <summary>
    /// Entries tab: live list of the saved entries of the active level in the order the game ranks them.
    /// </summary>
    public sealed class EntriesTab
    {
        private const int MaxRows = 500;

        public void Draw(DebuggerContext context)
        {
            DebuggerHelp.Info(context, "Shows the saved entries of the active level, sorted the way the game sorts them. It refreshes by itself when the save file changes, including while the game runs.");

            if (GUILayout.Button("Refresh")) context.Invalidate();

            if (context.SaveError != null)
            {
                if (context.SaveExists) DebuggerHelp.Error(context.SaveError);
                else DebuggerHelp.Warning($"{context.SaveError}. Generate entries or finish a level to create it.");
                return;
            }

            EditorGUILayout.LabelField("Levels in save", context.Levels.Count == 0 ? "none" : string.Join(", ", context.Levels));
            EditorGUILayout.LabelField($"Level {context.Level} ({context.Entries.Count} entries)", EditorStyles.boldLabel);

            if (context.Entries.Count == 0) DebuggerHelp.Warning($"Level {context.Level} has no saved entries.");

            var rows = Mathf.Min(context.Entries.Count, MaxRows);

            for (var i = 0; i < rows; i++)
            {
                var entry = context.Entries[i];

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"#{i + 1}", GUILayout.Width(40));
                    EditorGUILayout.LabelField(entry.name, GUILayout.MinWidth(80));
                    EditorGUILayout.LabelField(entry.id, GUILayout.MinWidth(80));
                    EditorGUILayout.LabelField(LeaderboardDebuggerService.FormatTime(entry.timescore), GUILayout.Width(100));
                }
            }

            if (context.Entries.Count > MaxRows) DebuggerHelp.Info(context, $"Showing the first {MaxRows} of {context.Entries.Count} entries.");
        }
    }
}
