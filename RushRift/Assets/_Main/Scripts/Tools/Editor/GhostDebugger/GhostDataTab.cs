using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.GhostDebugger
{
    /// <summary>
    /// Data tab: readout of the current frame and of the loaded ghost file.
    /// </summary>
    [Serializable]
    public sealed class GhostDataTab
    {
        public void Draw(GhostDebuggerContext context)
        {
            GhostDebuggerHelp.Info(context, "Shows what the ghost is doing at the current time and what the loaded ghost contains.");

            if (!context.HasRun)
            {
                GhostDebuggerHelp.Warning("No ghost run is loaded.");
                DrawValidity(context);
                return;
            }

            DrawFrame(context);
            EditorGUILayout.Space();
            DrawFile(context);
            EditorGUILayout.Space();
            DrawValidity(context);
        }

        private static void DrawFrame(GhostDebuggerContext context)
        {
            EditorGUILayout.LabelField("Current frame", EditorStyles.boldLabel);

            var t = context.Time;
            context.Bracket(t, out var previous, out var next, out var percent);
            var live = context.Source == GhostSource.Scene && context.HasSceneGhost;
            var position = live ? context.Player.GhostTransform.position : context.SamplePosition(t);
            var rotation = live ? context.Player.GhostTransform.rotation : context.SampleRotation(t);

            EditorGUILayout.LabelField("Time", $"{t:0.000} s");
            EditorGUILayout.LabelField("Sample index", context.CurrentIndex().ToString());
            EditorGUILayout.LabelField(live ? "Position (ghost)" : "Position (preview)", position.ToString("F3"));
            EditorGUILayout.LabelField(live ? "Rotation (ghost)" : "Rotation (preview)", rotation.eulerAngles.ToString("F2"));
            EditorGUILayout.LabelField("Speed", $"{context.SegmentSpeed(next):0.00} m/s");
            EditorGUILayout.LabelField("Previous sample", Describe(context, previous));
            EditorGUILayout.LabelField("Next sample", Describe(context, next));
            EditorGUILayout.LabelField("Between them", $"{percent * 100f:0.0} %");
        }

        private static void DrawFile(GhostDebuggerContext context)
        {
            EditorGUILayout.LabelField("Loaded ghost", EditorStyles.boldLabel);

            var run = context.Run;
            var path = context.Source == GhostSource.File ? context.FilePath : null;
            var info = string.IsNullOrEmpty(path) ? null : new FileInfo(path);

            EditorGUILayout.LabelField("Name", context.RunName);
            EditorGUILayout.LabelField("Size", info != null && info.Exists ? $"{info.Length / 1024f:0.0} KB" : "-");
            EditorGUILayout.LabelField("Samples", context.Frames.Count.ToString());
            EditorGUILayout.LabelField("Start", $"{context.Frames[0].time:0.000} s");
            EditorGUILayout.LabelField("Finish", $"{context.Frames[context.Frames.Count - 1].time:0.000} s");
            EditorGUILayout.LabelField("Run time in file", $"{run.durationSeconds:0.000} s");
            EditorGUILayout.LabelField("Format version", run.formatVersion.ToString());
            EditorGUILayout.LabelField("App version", string.IsNullOrEmpty(run.appVersion) ? "-" : run.appVersion);
            EditorGUILayout.LabelField("Level", string.IsNullOrEmpty(run.levelName) ? "-" : run.levelName);
            EditorGUILayout.LabelField("Recorded", string.IsNullOrEmpty(run.recordedAtUtc) ? "-" : run.recordedAtUtc);
        }

        private static void DrawValidity(GhostDebuggerContext context)
        {
            EditorGUILayout.LabelField("Validity", EditorStyles.boldLabel);

            if (context.RunError != null)
            {
                GhostDebuggerHelp.Error($"The file could not be read: {context.RunError}");
                return;
            }

            if (context.Run == null) return;

            if (context.RunIssues.Count == 0) EditorGUILayout.HelpBox("Valid: the game would play this ghost.", MessageType.Info);
            else GhostDebuggerHelp.Error("Invalid:\n" + string.Join("\n", context.RunIssues));
        }

        private static string Describe(GhostDebuggerContext context, int index)
        {
            var frame = context.Frames[index];
            return $"#{index}  {frame.time:0.000} s  {frame.position.ToString("F2")}";
        }
    }
}
