using System;
using UnityEditor;
using UnityEngine;

namespace Game.LeaderboardDebugger
{
    /// <summary>
    /// Entries tab: live list of the saved entries of the active level in the order the game ranks them.
    /// </summary>
    [Serializable]
    public sealed class EntriesTab
    {
        private const int MaxRows = 500;

        [SerializeField] private bool debug;

        public void Draw(DebuggerContext context)
        {
            DebuggerHelp.Info(context, "Shows the saved entries of the active level, sorted the way the game sorts them. It refreshes by itself when the save file changes. Use X to delete a single entry. Debug also shows each entry's id and the levels stored in the save.");

            using (new EditorGUILayout.HorizontalScope())
            {
                debug = GUILayout.Toggle(debug, "Debug (show ids)", GUILayout.Width(130));
                if (GUILayout.Button("Refresh", GUILayout.Width(80))) context.Invalidate();
            }

            if (context.SaveError != null)
            {
                if (context.SaveExists) DebuggerHelp.Error(context.SaveError);
                else DebuggerHelp.Warning($"{context.SaveError}. Generate entries or finish a level to create it.");
                return;
            }

            if (debug) EditorGUILayout.LabelField("Levels in save", context.Levels.Count == 0 ? "none" : string.Join(", ", context.Levels));

            EditorGUILayout.LabelField($"Level {context.Level} ({context.Entries.Count} entries)", EditorStyles.boldLabel);

            if (context.Entries.Count == 0) DebuggerHelp.Warning($"Level {context.Level} has no saved entries.");

            var rows = Mathf.Min(context.Entries.Count, MaxRows);

            for (var i = 0; i < rows; i++)
            {
                DrawRow(context, i);
            }

            if (context.Entries.Count > MaxRows) DebuggerHelp.Info(context, $"Showing the first {MaxRows} of {context.Entries.Count} entries.");
        }

        private void DrawRow(DebuggerContext context, int index)
        {
            var entry = context.Entries[index];

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"#{index + 1}", GUILayout.Width(40));
                EditorGUILayout.LabelField(entry.name, GUILayout.MinWidth(80));
                if (debug) EditorGUILayout.LabelField(entry.id, GUILayout.MinWidth(80));
                EditorGUILayout.LabelField(LeaderboardDebuggerService.FormatTime(entry.timescore), GUILayout.Width(100));

                if (GUILayout.Button("X", GUILayout.Width(22))) ConfirmDelete(context, entry, index + 1);
            }
        }

        private static void ConfirmDelete(DebuggerContext context, Score entry, int placement)
        {
            var message = $"Delete \"{entry.name}\" (#{placement}, {LeaderboardDebuggerService.FormatTime(entry.timescore)}) from level {context.Level}? This cannot be undone.";

            if (!EditorUtility.DisplayDialog("Delete entry", message, "Delete", "Cancel")) return;

            LeaderboardDebuggerService.DeleteEntry(context.Level, entry.id);
            context.Invalidate();
        }
    }
}
