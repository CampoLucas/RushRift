using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Game.Saves;
using UnityEngine;

namespace Game.LeaderboardDebugger
{
    public sealed class SaveBackup
    {
        private readonly byte[] bytes;

        public SaveBackup(byte[] bytes)
        {
            this.bytes = bytes;
        }

        public void Restore()
        {
            var path = LeaderboardDebuggerService.SavePath;

            if (bytes == null)
            {
                if (File.Exists(path)) File.Delete(path);
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, bytes);
        }
    }

    public static class LeaderboardDebuggerService
    {
        private static readonly FieldInfo PathField = typeof(SaveSystem).GetField("LeaderBoardPath", BindingFlags.NonPublic | BindingFlags.Static);

        public static string SavePath
        {
            get
            {
                if (PathField == null) throw new InvalidOperationException("SaveSystem.LeaderBoardPath was not found. The leaderboard path field was renamed or removed.");
                return (string)PathField.GetValue(null);
            }
        }

        public static string LibraryBackupPath => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Library", "LeaderboardDebuggerBackup.bin");

        public static bool SaveExists => File.Exists(SavePath);

        public static bool HasLibraryBackup => File.Exists(LibraryBackupPath);

        public static DateTime LibraryBackupTime => File.GetLastWriteTime(LibraryBackupPath);

        public static SaveBackup Capture()
        {
            return new SaveBackup(File.Exists(SavePath) ? File.ReadAllBytes(SavePath) : null);
        }

        public static LeaderBoardData Load()
        {
            return SaveSystem.LoadLeaderBoard();
        }

        public static void Save(LeaderBoardData data)
        {
            SaveSystem.SaveLeaderBoard(data);
        }

        public static void ClearAll()
        {
            SaveSystem.ResetLeaderBoard();
        }

        public static bool TryReadWithoutRepair(out LeaderBoardData data, out string error)
        {
            data = null;
            error = null;

            if (!File.Exists(SavePath))
            {
                error = $"No save file at {SavePath}";
                return false;
            }

            try
            {
                using var stream = new FileStream(SavePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                data = (LeaderBoardData)new BinaryFormatter().Deserialize(stream);
                return true;
            }
            catch (Exception e)
            {
                error = $"{e.GetType().Name}: {e.Message}";
                return false;
            }
        }

        public static List<int> GetLevels(LeaderBoardData data)
        {
            return data.Score.leaderBoardList.Keys.OrderBy(key => key).ToList();
        }

        public static List<Score> GetEntries(LeaderBoardData data, int level)
        {
            return data.Score.leaderBoardList.ContainsKey(level) ? data.GetLevelLeaderBoard(level) : new List<Score>();
        }

        public static void AddEntry(LeaderBoardData data, int level, string name, string id, float time)
        {
            data.AddTimeToLevel(level, new PlayerID { name = name, id = id }, time);
        }

        public static LeaderBoardData BuildData(int level, IEnumerable<Score> entries)
        {
            var data = new LeaderBoardData();
            foreach (var entry in entries) AddEntry(data, level, entry.name, entry.id, entry.timescore);
            return data;
        }

        public static void AddEntries(int level, IEnumerable<Score> entries)
        {
            var data = Load();
            foreach (var entry in entries) AddEntry(data, level, entry.name, entry.id, entry.timescore);
            Save(data);
        }

        public static void ReplaceLevel(int level, IEnumerable<Score> entries)
        {
            var data = Load();
            data.Score.leaderBoardList.Remove(level);
            foreach (var entry in entries) AddEntry(data, level, entry.name, entry.id, entry.timescore);
            Save(data);
        }

        public static void BackupToLibrary()
        {
            var bytes = File.Exists(SavePath) ? File.ReadAllBytes(SavePath) : Array.Empty<byte>();
            File.WriteAllBytes(LibraryBackupPath, bytes);
        }

        public static void RestoreFromLibrary()
        {
            var bytes = File.ReadAllBytes(LibraryBackupPath);
            new SaveBackup(bytes.Length == 0 ? null : bytes).Restore();
        }

        public static void DeleteLibraryBackup()
        {
            if (File.Exists(LibraryBackupPath)) File.Delete(LibraryBackupPath);
        }

        public static void SaveSnapshot(string path)
        {
            Load();
            File.Copy(SavePath, path, true);
        }

        public static void LoadSnapshot(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
            File.Copy(path, SavePath, true);
        }

        public static void DeleteSave()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
        }

        public static void WriteGarbage()
        {
            var bytes = new byte[256];
            new System.Random(12345).NextBytes(bytes);
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
            File.WriteAllBytes(SavePath, bytes);
        }

        public static void Truncate()
        {
            var bytes = File.ReadAllBytes(SavePath);
            File.WriteAllBytes(SavePath, bytes.Take(bytes.Length / 2).ToArray());
        }

        public static void FlipBytes()
        {
            var bytes = File.ReadAllBytes(SavePath);
            var step = Math.Max(1, bytes.Length / 8);
            for (var i = step; i < bytes.Length; i += step) bytes[i] ^= 0xFF;
            File.WriteAllBytes(SavePath, bytes);
        }

        public static string FormatTime(float time)
        {
            return time.ToString("R", CultureInfo.InvariantCulture);
        }

        public static string Describe(LeaderBoardData data)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Version: {data.Version}");

            foreach (var level in GetLevels(data))
            {
                var entries = data.Score.leaderBoardList[level];
                builder.AppendLine($"Level {level} ({entries.Count} entries)");

                foreach (var pair in entries)
                {
                    builder.AppendLine($"  key=\"{pair.Key}\" name=\"{pair.Value.name}\" id=\"{pair.Value.id}\" time={FormatTime(pair.Value.timescore)}");
                }
            }

            return builder.ToString();
        }

        public static string HexDump(byte[] bytes, int maxBytes)
        {
            var builder = new StringBuilder();
            var length = Math.Min(bytes.Length, maxBytes);

            for (var offset = 0; offset < length; offset += 16)
            {
                var count = Math.Min(16, length - offset);
                builder.Append(offset.ToString("X6")).Append("  ");

                for (var i = 0; i < 16; i++)
                {
                    builder.Append(i < count ? bytes[offset + i].ToString("X2") : "  ").Append(' ');
                }

                builder.Append(' ');

                for (var i = 0; i < count; i++)
                {
                    var value = bytes[offset + i];
                    builder.Append(value >= 32 && value < 127 ? (char)value : '.');
                }

                builder.AppendLine();
            }

            if (bytes.Length > maxBytes) builder.AppendLine($"... {bytes.Length - maxBytes} more bytes not shown");

            return builder.ToString();
        }

        public static string Compare(LeaderBoardData expected, LeaderBoardData actual)
        {
            var expectedLevels = expected.Score.leaderBoardList;
            var actualLevels = actual.Score.leaderBoardList;

            if (expectedLevels.Count != actualLevels.Count) return $"Level count differs: expected {expectedLevels.Count}, got {actualLevels.Count}";

            foreach (var level in expectedLevels)
            {
                if (!actualLevels.TryGetValue(level.Key, out var other)) return $"Level {level.Key} is missing";
                if (level.Value.Count != other.Count) return $"Level {level.Key} entry count differs: expected {level.Value.Count}, got {other.Count}";

                foreach (var entry in level.Value)
                {
                    if (!other.TryGetValue(entry.Key, out var score)) return $"Entry key \"{entry.Key}\" is missing in level {level.Key}";

                    var original = entry.Value;
                    if (score.name != original.name || score.id != original.id || !score.timescore.Equals(original.timescore))
                    {
                        return $"Entry \"{entry.Key}\" in level {level.Key} differs: expected ({original.name}, {original.id}, {FormatTime(original.timescore)}), got ({score.name}, {score.id}, {FormatTime(score.timescore)})";
                    }
                }
            }

            return null;
        }

        public static string CompareOrder(IList<Score> actual, IEnumerable<Score> source)
        {
            var expected = source.OrderBy(score => score.timescore).ToList();

            if (expected.Count != actual.Count) return $"Entry count differs: expected {expected.Count}, got {actual.Count}";

            for (var i = 0; i < expected.Count; i++)
            {
                if (!expected[i].timescore.Equals(actual[i].timescore))
                {
                    return $"Position {i + 1}: expected time {FormatTime(expected[i].timescore)}, got {FormatTime(actual[i].timescore)}";
                }
            }

            var missing = expected.Select(score => score.id).Except(actual.Select(score => score.id)).FirstOrDefault();
            if (missing != null) return $"Entry with id \"{missing}\" is missing from the sorted list";

            return null;
        }
    }
}
