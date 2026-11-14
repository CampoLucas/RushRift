using System;
using System.Collections.Generic;
using System.IO;
using _Main.Scripts.Ghost;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Game.GhostDebugger
{
    /// <summary>
    /// Files tab: sandbox ghosts by name, JSON export, the real save folder behind explicit buttons, forced bad files and run control.
    /// </summary>
    [Serializable]
    public sealed class GhostFilesTab
    {
        [SerializeField] private string saveName = "my_ghost";
        [SerializeField] private string forcedResult = "";
        [SerializeField] private float fakeTime;
        [SerializeField] private bool realScanned;

        private List<string> sandboxNames = new List<string>();
        private List<string> realNames = new List<string>();
        private int listedRevision = -1;

        public void Draw(GhostDebuggerContext context)
        {
            GhostDebuggerHelp.Info(context, "Save, load, switch and export ghosts, force broken files on purpose, and push the run into situations. Your real save folder is only changed by buttons that start with 'Real save'.");

            if (listedRevision != context.Revision) Refresh(context);

            DrawSaveFolder(context);
            EditorGUILayout.Space();
            DrawSandbox(context);
            EditorGUILayout.Space();
            DrawExport(context);
            EditorGUILayout.Space();
            DrawForced(context);
            EditorGUILayout.Space();
            DrawRunControl(context);
            EditorGUILayout.Space();
            DrawReal(context);
        }

        private void Refresh(GhostDebuggerContext context)
        {
            listedRevision = context.Revision;
            sandboxNames = GhostDebuggerService.ListGhosts(GhostDebuggerService.SandboxFolder);
            if (realScanned) realNames = GhostDebuggerService.ListGhosts(GhostDebuggerService.RealFolder);
        }

        private static void DrawSaveFolder(GhostDebuggerContext context)
        {
            EditorGUILayout.LabelField("Where the game saves and loads ghosts", EditorStyles.boldLabel);
            context.UseSandbox = EditorGUILayout.ToggleLeft("Use the sandbox folder while this window is open", context.UseSandbox);
            EditorGUILayout.LabelField("Sandbox", GhostDebuggerService.SandboxFolder, EditorStyles.miniLabel);

            GhostDebuggerHelp.Info(context, "With the sandbox on, the game records and loads best ghosts from the sandbox, so finishing a level while testing never overwrites your real ghosts. Takes effect for the next level load.");
            if (!context.UseSandbox) GhostDebuggerHelp.Warning($"The game is using the real save folder: {GhostDebuggerService.RealFolder}");
        }

        private void DrawSandbox(GhostDebuggerContext context)
        {
            EditorGUILayout.LabelField("Saved ghosts (sandbox)", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                saveName = EditorGUILayout.TextField("Save active as", saveName);
                using (new EditorGUI.DisabledScope(!context.HasRun || string.IsNullOrWhiteSpace(saveName)))
                {
                    if (GUILayout.Button("Save", GUILayout.Width(60))) SaveActive(context, GhostDebuggerService.PathFor(GhostDebuggerService.SandboxFolder, saveName.Trim()));
                }
            }

            if (GUILayout.Button("Import JSON into the sandbox")) ImportJson(context);

            if (sandboxNames.Count == 0) EditorGUILayout.LabelField("No saved ghosts yet.", EditorStyles.miniLabel);

            foreach (var name in sandboxNames)
            {
                var path = GhostDebuggerService.PathFor(GhostDebuggerService.SandboxFolder, name);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(name);
                    if (GUILayout.Button(new GUIContent("Use", "Play it on the scene ghost, or preview it if there is none"), GUILayout.Width(50))) Use(context, path);
                    if (GUILayout.Button("Export", GUILayout.Width(60))) ExportFile(context, path, name);
                    if (GUILayout.Button("Delete", GUILayout.Width(60)) && EditorUtility.DisplayDialog("Delete ghost", $"Delete the sandbox ghost '{name}'?", "Delete", "Cancel"))
                    {
                        File.Delete(path);
                        Refresh(context);
                    }
                }
            }
        }

        private static void DrawExport(GhostDebuggerContext context)
        {
            EditorGUILayout.LabelField("Export to readable JSON", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Folder", context.ExportFolder);
                if (GUILayout.Button("Browse", GUILayout.Width(60)))
                {
                    var picked = EditorUtility.OpenFolderPanel("Ghost export folder", context.ExportFolder, "");
                    if (!string.IsNullOrEmpty(picked)) context.SetExportFolder(picked);
                }

                if (GUILayout.Button("Default", GUILayout.Width(60))) context.SetExportFolder("");
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!context.HasRun))
                {
                    if (GUILayout.Button("Export active ghost")) Export(context, context.Run, context.RunName);
                }

                if (GUILayout.Button("Open folder", GUILayout.Width(90)))
                {
                    Directory.CreateDirectory(context.ExportFolder);
                    EditorUtility.RevealInFinder(context.ExportFolder);
                }
            }
        }

        private void DrawForced(GhostDebuggerContext context)
        {
            EditorGUILayout.LabelField("Force bad files", EditorStyles.boldLabel);
            GhostDebuggerHelp.Info(context, "Writes a broken ghost into a temp folder for every loaded scene and loads it the way the game does. Shows whether it was rejected, logged an error, disabled the scene ghost or crashed.");

            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (BadGhostFile kind in Enum.GetValues(typeof(BadGhostFile)))
                {
                    if (GUILayout.Button($"Load {kind.ToString().ToLower()}")) forcedResult = GhostDebuggerService.ForceLoad(kind, context.Run, context.Player);
                }
            }

            if (!string.IsNullOrEmpty(forcedResult))
            {
                EditorGUILayout.HelpBox(forcedResult, forcedResult.Contains("CRASH") || forcedResult.Contains("VALID") ? MessageType.Error : MessageType.Info);
            }

            if (context.Player && GUILayout.Button("Reload the best ghost on the scene ghost")) context.Player.LoadBestGhost();
        }

        private void DrawRunControl(GhostDebuggerContext context)
        {
            EditorGUILayout.LabelField("Run control (Play Mode)", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                GhostDebuggerHelp.Info(context, "Enter Play Mode to end the run early, change the recording time or respawn.");
                return;
            }

            if (GUILayout.Button("End the run early (win)") && EditorUtility.DisplayDialog("End run", $"Send the game's win event now? The recorder will save this run if it is a new best, into:\n{(context.UseSandbox ? GhostDebuggerService.SandboxFolder : GhostDebuggerService.RealFolder)}", "End run", "Cancel"))
            {
                GlobalEvents.GameOver.NotifyAll(true);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                fakeTime = EditorGUILayout.FloatField("Recording time", fakeTime);
                using (new EditorGUI.DisabledScope(!context.Recorder || context.Recorder.CurrentRun == null))
                {
                    if (GUILayout.Button("Set", GUILayout.Width(50))) context.Recorder.CurrentRun.durationSeconds = Mathf.Max(0f, fakeTime);
                }
            }

            if (context.Recorder && context.Recorder.CurrentRun != null)
            {
                EditorGUILayout.LabelField("Recording now", $"{context.Recorder.CurrentRun.durationSeconds:0.000} s, {context.Recorder.CurrentRun.frames.Count} samples", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField("Recording now", "nothing is being recorded", EditorStyles.miniLabel);
            }

            if (GUILayout.Button("Respawn the player")) PlayerSpawner.RespawnPlayerAsync(default).Forget();
            GhostDebuggerHelp.Info(context, "The game has no checkpoints, so there is no checkpoint reset. Respawn moves the player back to the level spawn.");
        }

        private void DrawReal(GhostDebuggerContext context)
        {
            EditorGUILayout.LabelField("Real save folder", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Path", GhostDebuggerService.RealFolder, EditorStyles.miniLabel);

            if (GUILayout.Button(realScanned ? "Rescan real save folder" : "Scan real save folder (read only)"))
            {
                realScanned = true;
                realNames = GhostDebuggerService.ListGhosts(GhostDebuggerService.RealFolder);
            }

            if (!realScanned) return;
            if (realNames.Count == 0) EditorGUILayout.LabelField("No ghosts in the real save folder.", EditorStyles.miniLabel);

            foreach (var name in realNames)
            {
                var path = GhostDebuggerService.PathFor(GhostDebuggerService.RealFolder, name);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(name);
                    if (GUILayout.Button("Copy to sandbox", GUILayout.Width(110)))
                    {
                        Directory.CreateDirectory(GhostDebuggerService.SandboxFolder);
                        File.Copy(path, GhostDebuggerService.PathFor(GhostDebuggerService.SandboxFolder, name), true);
                        Refresh(context);
                    }

                    if (GUILayout.Button("Real save: Delete", GUILayout.Width(120)) && EditorUtility.DisplayDialog("Delete REAL ghost", $"Permanently delete the real save file?\n\n{path}", "Delete real file", "Cancel"))
                    {
                        File.Delete(path);
                        realNames = GhostDebuggerService.ListGhosts(GhostDebuggerService.RealFolder);
                    }

                    using (new EditorGUI.DisabledScope(!context.HasRun))
                    {
                        if (GUILayout.Button("Real save: Overwrite with active", GUILayout.Width(200)) && EditorUtility.DisplayDialog("Overwrite REAL ghost", $"Replace the real save file with the active ghost?\n\n{path}", "Overwrite real file", "Cancel"))
                        {
                            SaveActive(context, path);
                        }
                    }
                }
            }
        }

        private void SaveActive(GhostDebuggerContext context, string path)
        {
            var data = GhostDebuggerService.Clone(context.Run);
            if (data.formatVersion == 0) data.formatVersion = GhostRecorder.CurrentFormatVersion;
            GhostDebuggerService.Write(data, path);
            Refresh(context);
        }

        private void ImportJson(GhostDebuggerContext context)
        {
            var source = EditorUtility.OpenFilePanel("Import ghost JSON", context.ExportFolder, "json");
            if (string.IsNullOrEmpty(source)) return;

            if (!GhostDebuggerService.TryRead(source, out var data, out var error))
            {
                EditorUtility.DisplayDialog("Import ghost", $"Could not read the file:\n{error}", "OK");
                return;
            }

            var name = Path.GetFileNameWithoutExtension(source).Replace(".ghost", "");
            GhostDebuggerService.Write(data, GhostDebuggerService.PathFor(GhostDebuggerService.SandboxFolder, name));
            Refresh(context);
        }

        private static void Use(GhostDebuggerContext context, string path)
        {
            if (!GhostDebuggerService.TryRead(path, out var data, out var error))
            {
                EditorUtility.DisplayDialog("Use ghost", $"Could not read the file:\n{error}", "OK");
                return;
            }

            if (context.Player && GhostDebuggerService.Validate(data).Count > 0)
            {
                EditorUtility.DisplayDialog("Use ghost", "This ghost is invalid, so it is opened as a preview instead of being played on the scene ghost. See the Data tab.", "OK");
                context.SelectFile(path);
                return;
            }

            context.UseRun(data, path);
        }

        private static void ExportFile(GhostDebuggerContext context, string path, string name)
        {
            if (GhostDebuggerService.TryRead(path, out var data, out var error)) Export(context, data, name);
            else EditorUtility.DisplayDialog("Export ghost", $"Could not read the file:\n{error}", "OK");
        }

        private static void Export(GhostDebuggerContext context, GhostRecorder.GhostRunData data, string name)
        {
            var safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars())).Replace(GhostDebuggerService.Extension, "");
            var path = Path.Combine(context.ExportFolder, $"{safeName}_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            GhostRunJson.Export(data, path);
            Debug.Log($"[GhostDebugger] Exported ghost to {path}");
        }
    }
}
