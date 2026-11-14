using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.GhostDebugger
{
    /// <summary>
    /// Runs ghost checks one at a time across editor updates and shows their results; a failed result jumps the timeline when clicked.
    /// </summary>
    public sealed class GhostCheckListView
    {
        private readonly Dictionary<string, GhostCheckResult> results = new Dictionary<string, GhostCheckResult>();
        private readonly Queue<GhostCheck> queue = new Queue<GhostCheck>();
        private IEnumerator running;
        private string runningName;
        private GhostDebuggerContext runningContext;

        public bool IsRunning => running != null;

        public void Draw(GhostDebuggerContext context)
        {
            GhostDebuggerHelp.Info(context, "Each check runs one scenario on the loaded ghost and shows the measured value. Click a failed result to move the timeline to where it failed. Start and Pause need Play Mode with the scene ghost.");

            DrawSettings(context);
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(IsRunning))
                {
                    if (GUILayout.Button("Run all checks")) RunAll(context);
                }

                using (new EditorGUI.DisabledScope(!IsRunning))
                {
                    if (GUILayout.Button("Stop", GUILayout.Width(60))) Stop();
                }
            }

            var known = GhostChecks.All.Where(check => results.ContainsKey(check.Name)).Select(check => results[check.Name]).ToList();
            EditorGUILayout.LabelField($"Passed: {known.Count(result => result.Passed)}   Failed: {known.Count(result => !result.Passed)}   Not run: {GhostChecks.All.Count - known.Count}", EditorStyles.boldLabel);
            if (IsRunning) EditorGUILayout.HelpBox($"Running {runningName}...", MessageType.Info);

            foreach (var check in GhostChecks.All) DrawCheck(context, check);
        }

        public void Update()
        {
            if (running == null) return;

            try
            {
                if (!running.MoveNext())
                {
                    Finish(new GhostCheckResult(runningName, false, "The check ended without a result."));
                    return;
                }

                if (running.Current is GhostCheckResult result) Finish(result);
            }
            catch (Exception e)
            {
                Finish(new GhostCheckResult(runningName, false, $"{e.GetType().Name}: {e.Message}"));
            }
        }

        public void Stop()
        {
            queue.Clear();
            running = null;
        }

        private static void DrawSettings(GhostDebuggerContext context)
        {
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            context.FidelityTolerance = EditorGUILayout.FloatField("Fidelity tolerance (m)", context.FidelityTolerance);
            context.RotationTolerance = EditorGUILayout.FloatField("Rotation tolerance (°)", context.RotationTolerance);
            context.WallMask = LayerMaskField("Path: wall layers", context.WallMask);
            context.GroundMask = LayerMaskField("Path: ground layers", context.GroundMask);
            context.ProbeHeight = EditorGUILayout.FloatField("Path: probe height (m)", context.ProbeHeight);
            context.ProbeRadius = EditorGUILayout.FloatField("Path: probe radius (m)", context.ProbeRadius);
            context.GroundDistance = EditorGUILayout.FloatField("Path: max ground distance (m)", context.GroundDistance);
        }

        private void DrawCheck(GhostDebuggerContext context, GhostCheck check)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(IsRunning))
                {
                    if (GUILayout.Button("Run", GUILayout.Width(40))) Begin(context, check);
                }

                EditorGUILayout.LabelField(new GUIContent(check.Name, check.Description));
            }

            if (context.ShowHelp) EditorGUILayout.LabelField(check.Description, EditorStyles.wordWrappedMiniLabel);
            if (!results.TryGetValue(check.Name, out var shown)) return;

            var canJump = !shown.Passed && shown.FailTime >= 0f && context.HasRun;
            EditorGUILayout.HelpBox($"{(shown.Passed ? "PASS" : "FAIL")}: {shown.Message}{(canJump ? $"\nClick to jump to {shown.FailTime:0.000} s." : "")}", shown.Passed ? MessageType.Info : MessageType.Error);

            if (!canJump) return;

            var rect = GUILayoutUtility.GetLastRect();
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                context.JumpTo(shown.FailTime);
                Event.current.Use();
            }
        }

        private void RunAll(GhostDebuggerContext context)
        {
            queue.Clear();
            foreach (var check in GhostChecks.All) queue.Enqueue(check);
            runningContext = context;
            Next();
        }

        private void Begin(GhostDebuggerContext context, GhostCheck check)
        {
            queue.Clear();
            queue.Enqueue(check);
            runningContext = context;
            Next();
        }

        private void Next()
        {
            if (queue.Count == 0)
            {
                running = null;
                return;
            }

            var check = queue.Dequeue();
            runningName = check.Name;
            running = check.Run(runningContext);
        }

        private void Finish(GhostCheckResult result)
        {
            results[result.Name] = result;
            Next();
        }

        private static LayerMask LayerMaskField(string label, LayerMask mask)
        {
            var field = EditorGUILayout.MaskField(label, UnityEditorInternal.InternalEditorUtility.LayerMaskToConcatenatedLayersMask(mask), UnityEditorInternal.InternalEditorUtility.layers);
            return UnityEditorInternal.InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(field);
        }
    }
}
