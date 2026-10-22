using System;
using System.IO;
using System.Reflection;
using _Main.Scripts.Ghost;
using Game.Saves;
using UnityEngine;

namespace Game.ConsoleCommands.Tests
{
    /// <summary>
    /// Copies the settings, save and leaderboard files to the Library folder and puts them back on Dispose.
    /// Ghosts are redirected to a temporary folder for the same lifetime.
    /// </summary>
    public sealed class ConsoleTestDataBackup : IDisposable
    {
        public const string SettingsField = "SettingsPath";
        public const string GameField = "GamePath";
        public const string LeaderboardField = "LeaderBoardPath";

        private const string MissingSuffix = ".missing";

        private static readonly string[] PathFields = { SettingsField, GameField, LeaderboardField };

        private readonly string _previousGhostFolder;
        private bool _disposed;

        public string GhostFolder { get; }

        public static string BackupFolder => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Library", "ConsoleCommandsTestBackup");

        public ConsoleTestDataBackup()
        {
            if (Directory.Exists(BackupFolder)) RestoreFiles();

            Directory.CreateDirectory(BackupFolder);

            foreach (var field in PathFields)
            {
                var path = GetPath(field);
                var copy = Path.Combine(BackupFolder, field);

                if (File.Exists(path)) File.Copy(path, copy, true);
                else File.WriteAllText(copy + MissingSuffix, "");
            }

            _previousGhostFolder = GhostRecorder.FolderOverride;
            GhostFolder = Path.Combine(Path.GetTempPath(), "RushRiftConsoleTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(GhostFolder);
            GhostRecorder.FolderOverride = GhostFolder;
        }

        public static string GetPath(string field)
        {
            var info = typeof(SaveSystem).GetField(field, BindingFlags.NonPublic | BindingFlags.Static);
            if (info == null) throw new InvalidOperationException($"SaveSystem.{field} was not found. The save path field was renamed or removed.");
            return (string)info.GetValue(null);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            RestoreFiles();
            GhostRecorder.FolderOverride = _previousGhostFolder;
            if (Directory.Exists(GhostFolder)) Directory.Delete(GhostFolder, true);
        }

        private static void RestoreFiles()
        {
            foreach (var field in PathFields)
            {
                var path = GetPath(field);
                var copy = Path.Combine(BackupFolder, field);

                if (File.Exists(copy))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.Copy(copy, path, true);
                }
                else if (File.Exists(copy + MissingSuffix) && File.Exists(path))
                {
                    File.Delete(path);
                }
            }

            Directory.Delete(BackupFolder, true);
        }
    }
}
