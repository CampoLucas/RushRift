using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using _Main.Scripts.Ghost;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.GhostDebugger
{
    public enum BadGhostFile
    {
        Corrupted,
        Empty,
        Outdated
    }

    /// <summary>
    /// Ghost file operations for the debugger: folders, reading, validation, saving and forced bad files.
    /// </summary>
    public static class GhostDebuggerService
    {
        public const string Extension = ".ghost.json";

        public static string RealFolder => Path.Combine(Application.persistentDataPath, "ghosts");
        public static string SandboxFolder => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Library", "GhostDebugger", "Ghosts"));
        public static string DefaultExportFolder => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "GhostExports"));

        private static string ForcedFolder => Path.Combine(Application.temporaryCachePath, "GhostDebugger", "Forced");

        public static List<string> ListGhosts(string folder)
        {
            if (!Directory.Exists(folder)) return new List<string>();

            return Directory.GetFiles(folder, "*" + Extension)
                .Select(path => Path.GetFileName(path).Substring(0, Path.GetFileName(path).Length - Extension.Length))
                .OrderBy(name => name)
                .ToList();
        }

        public static string PathFor(string folder, string name)
        {
            return Path.Combine(folder, name + Extension);
        }

        public static bool TryRead(string path, out GhostRecorder.GhostRunData data, out string error)
        {
            data = null;
            error = null;

            try
            {
                if (!File.Exists(path))
                {
                    error = "The file does not exist.";
                    return false;
                }

                data = JsonUtility.FromJson<GhostRecorder.GhostRunData>(File.ReadAllText(path));
                if (data == null) error = "The file is empty or is not a ghost.";
                return data != null;
            }
            catch (Exception e)
            {
                error = $"{e.GetType().Name}: {e.Message}";
                return false;
            }
        }

        public static List<string> Validate(GhostRecorder.GhostRunData data)
        {
            var issues = new List<string>();
            if (data == null)
            {
                issues.Add("No run data.");
                return issues;
            }

            if (data.frames == null || data.frames.Count < 2) issues.Add("Fewer than 2 samples, so the game will not play it.");
            if (data.durationSeconds <= 0.25f) issues.Add($"Run time {data.durationSeconds:0.###} s is not above 0.25 s, so the game will not play it.");
            if (data.formatVersion < GhostRecorder.MinFormatVersion || data.formatVersion > GhostRecorder.CurrentFormatVersion) issues.Add($"Format version {data.formatVersion} is not supported (expected {GhostRecorder.MinFormatVersion} to {GhostRecorder.CurrentFormatVersion}).");

            if (data.frames == null) return issues;

            for (var i = 0; i < data.frames.Count; i++)
            {
                var frame = data.frames[i];
                if (!IsFinite(frame.time) || !IsFinite(frame.position.x) || !IsFinite(frame.position.y) || !IsFinite(frame.position.z) || !IsFinite(frame.rotation.x) || !IsFinite(frame.rotation.y) || !IsFinite(frame.rotation.z) || !IsFinite(frame.rotation.w))
                {
                    issues.Add($"Sample {i} has a NaN or infinite value.");
                    break;
                }
            }

            return issues;
        }

        public static void Write(GhostRecorder.GhostRunData data, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(data));
        }

        public static GhostRecorder.GhostRunData Clone(GhostRecorder.GhostRunData data)
        {
            return JsonUtility.FromJson<GhostRecorder.GhostRunData>(JsonUtility.ToJson(data));
        }

        public static string ForceLoad(BadGhostFile kind, GhostRecorder.GhostRunData template, GhostPlayer scenePlayer)
        {
            if (Directory.Exists(ForcedFolder)) Directory.Delete(ForcedFolder, true);
            Directory.CreateDirectory(ForcedFolder);

            var levelNames = LoadedSceneNames();
            foreach (var level in levelNames)
            {
                WriteBadFile(kind, template, Path.Combine(ForcedFolder, $"level_{level}{Extension}"));
            }

            var previousOverride = GhostRecorder.FolderOverride;
            var errors = new List<string>();
            var exceptions = new List<string>();

            void OnLog(string condition, string stackTrace, LogType type)
            {
                if (type == LogType.Error) errors.Add(condition);
                else if (type == LogType.Exception) exceptions.Add(condition);
            }

            var lines = new List<string> { $"Forced a {kind.ToString().ToLower()} file for: {string.Join(", ", levelNames)}" };
            Application.logMessageReceived += OnLog;
            GhostRecorder.FolderOverride = ForcedFolder;

            try
            {
                var valid = GhostRecorder.TryLoadBestGhostForLevel(levelNames[0], out _, out _);
                lines.Add($"Loader returned: {(valid ? "VALID (the bad file was accepted)" : "invalid")}");

                if (scenePlayer)
                {
                    scenePlayer.LoadBestGhost();
                    lines.Add(scenePlayer.LoadedRun == null ? "Scene ghost: disabled (no run loaded, hidden)." : "Scene ghost: still has a run loaded.");
                }
                else
                {
                    lines.Add("Scene ghost: none in the scene, only the loader was tested.");
                }
            }
            catch (Exception e)
            {
                lines.Add($"CRASH: {e.GetType().Name}: {e.Message}");
            }
            finally
            {
                GhostRecorder.FolderOverride = previousOverride;
                Application.logMessageReceived -= OnLog;
            }

            lines.Add(errors.Count == 0 ? "Errors logged: none (the failure was silent)." : $"Errors logged: {errors.Count}. First: {errors[0]}");
            lines.Add(exceptions.Count == 0 ? "Exceptions logged: none." : $"Exceptions logged: {exceptions.Count}. First: {exceptions[0]}");
            return string.Join("\n", lines);
        }

        public static GhostRecorder.GhostRunData StraightLine()
        {
            var data = new GhostRecorder.GhostRunData { levelName = "GhostDebugger", durationSeconds = 2f, formatVersion = GhostRecorder.CurrentFormatVersion, appVersion = Application.version, recordedAtUtc = DateTime.UtcNow.ToString("o") };
            for (var i = 0; i <= 20; i++)
            {
                data.frames.Add(new GhostRecorder.GhostFrame { time = i * 0.1f, position = new Vector3(i, 0f, 0f), rotation = Quaternion.identity });
            }

            return data;
        }

        private static void WriteBadFile(BadGhostFile kind, GhostRecorder.GhostRunData template, string path)
        {
            switch (kind)
            {
                case BadGhostFile.Empty:
                    File.WriteAllBytes(path, Array.Empty<byte>());
                    break;
                case BadGhostFile.Corrupted:
                    var bytes = new byte[2048];
                    new System.Random(7).NextBytes(bytes);
                    File.WriteAllBytes(path, bytes);
                    break;
                case BadGhostFile.Outdated:
                    var outdated = Clone(template ?? StraightLine());
                    outdated.formatVersion = GhostRecorder.MinFormatVersion - 1;
                    File.WriteAllText(path, JsonUtility.ToJson(outdated));
                    break;
            }
        }

        private static List<string> LoadedSceneNames()
        {
            var names = new List<string>();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                names.Add(SceneManager.GetSceneAt(i).name);
            }

            return names;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
