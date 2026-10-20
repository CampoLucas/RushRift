using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.LeaderboardDebugger
{
    /// <summary>
    /// Data tab: manages the leaderboard save file (backup, snapshots, clear) and shows its raw contents.
    /// </summary>
    [Serializable]
    public sealed class DataTab
    {
        private const int HexBytes = 4096;

        [SerializeField] private bool showRaw;
        [SerializeField] private bool showHex;

        private int rawRevision = -1;
        private string rawDescription = "";
        private string rawHex = "";

        public void Draw(DebuggerContext context)
        {
            DebuggerHelp.Info(context, "Manage the leaderboard save file: back it up, restore it, save or load snapshots, clear it and inspect exactly what is stored.");

            DrawFile(context);
            EditorGUILayout.Space();
            DrawBackup(context);
            EditorGUILayout.Space();
            DrawSnapshots(context);
            EditorGUILayout.Space();
            DrawClear(context);
            EditorGUILayout.Space();
            DrawRaw(context);
        }

        private static void DrawFile(DebuggerContext context)
        {
            EditorGUILayout.LabelField("Save file", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Path", LeaderboardDebuggerService.SavePath);

            if (!context.SaveExists) DebuggerHelp.Warning("There is no save file at this path yet.");
            else if (context.SaveError != null) DebuggerHelp.Error($"The save cannot be read: {context.SaveError}");
        }

        private static void DrawBackup(DebuggerContext context)
        {
            EditorGUILayout.LabelField("Backup", EditorStyles.boldLabel);
            DebuggerHelp.Info(context, "A backup copies the current save into the project's Library folder, so you can run destructive tests and return to it. It survives domain reloads.");
            EditorGUILayout.LabelField("Stored backup", LeaderboardDebuggerService.HasLibraryBackup ? LeaderboardDebuggerService.LibraryBackupTime.ToString("g") : "none");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Back up current data")) LeaderboardDebuggerService.BackupToLibrary();

                using (new EditorGUI.DisabledScope(!LeaderboardDebuggerService.HasLibraryBackup))
                {
                    if (GUILayout.Button("Restore backup")) LeaderboardDebuggerService.RestoreFromLibrary();
                    if (GUILayout.Button("Delete backup")) LeaderboardDebuggerService.DeleteLibraryBackup();
                }
            }

            if (!LeaderboardDebuggerService.HasLibraryBackup) DebuggerHelp.Info(context, "There is no backup yet, so Restore and Delete are disabled.");
        }

        private static void DrawSnapshots(DebuggerContext context)
        {
            EditorGUILayout.LabelField("Snapshots", EditorStyles.boldLabel);
            DebuggerHelp.Info(context, "A snapshot is a copy of the save file stored wherever you choose. Loading one replaces the current save.");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save snapshot to file"))
                {
                    var path = EditorUtility.SaveFilePanel("Save leaderboard snapshot", "", "leaderboard_snapshot", "save");
                    if (!string.IsNullOrEmpty(path)) LeaderboardDebuggerService.SaveSnapshot(path);
                }

                if (GUILayout.Button("Load snapshot from file"))
                {
                    var path = EditorUtility.OpenFilePanel("Load leaderboard snapshot", "", "save");
                    if (!string.IsNullOrEmpty(path)) LeaderboardDebuggerService.LoadSnapshot(path);
                }
            }

            if (!LeaderboardDebuggerService.HasLibraryBackup) DebuggerHelp.Warning("Loading a snapshot overwrites the current save and you have no backup of it.");
        }

        private static void DrawClear(DebuggerContext context)
        {
            EditorGUILayout.LabelField("Clear", EditorStyles.boldLabel);
            DebuggerHelp.Info(context, "Removes every entry on every level. A confirmation dialog appears first.");

            if (GUILayout.Button("Clear all leaderboard data"))
            {
                if (EditorUtility.DisplayDialog("Clear leaderboard", "This deletes every entry on every level from the saved leaderboard. Continue?", "Clear", "Cancel"))
                {
                    LeaderboardDebuggerService.ClearAll();
                }
            }

            if (!context.SaveExists || context.Levels.Count == 0) DebuggerHelp.Info(context, "There is nothing to clear right now.");
        }

        private void DrawRaw(DebuggerContext context)
        {
            showRaw = EditorGUILayout.Foldout(showRaw, "Raw saved data", true);

            if (!showRaw) return;

            DebuggerHelp.Info(context, "Exactly what is stored: a readable listing of the deserialized data and a hex dump of the first 4096 bytes of the binary file.");

            if (context.Data == null)
            {
                DebuggerHelp.Warning("There is nothing to show because the save is missing or unreadable.");
                return;
            }

            if (rawRevision != context.Revision) RefreshRaw(context);

            EditorGUILayout.SelectableLabel(rawDescription, EditorStyles.textArea, GUILayout.MinHeight(Mathf.Min(300, 16 * (rawDescription.Count(character => character == '\n') + 2))));
            showHex = EditorGUILayout.Foldout(showHex, "Hex dump of the binary file");
            if (showHex) EditorGUILayout.SelectableLabel(rawHex, EditorStyles.textArea, GUILayout.MinHeight(300));
        }

        private void RefreshRaw(DebuggerContext context)
        {
            rawRevision = context.Revision;
            rawDescription = LeaderboardDebuggerService.Describe(context.Data);
            rawHex = LeaderboardDebuggerService.HexDump(File.ReadAllBytes(LeaderboardDebuggerService.SavePath), HexBytes);
        }
    }
}
