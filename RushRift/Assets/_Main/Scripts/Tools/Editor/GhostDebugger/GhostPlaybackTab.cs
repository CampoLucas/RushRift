using System;
using UnityEditor;
using UnityEngine;

namespace Game.GhostDebugger
{
    /// <summary>
    /// Playback tab: takes over the ghost timeline and moves it to absolute times or sample indices.
    /// </summary>
    [Serializable]
    public sealed class GhostPlaybackTab
    {
        private static readonly float[] Speeds = { 0.1f, 0.25f, 0.5f, 1f, 2f, 4f };
        private static readonly string[] SpeedLabels = { "0.1x", "0.25x", "0.5x", "1x", "2x", "4x" };

        [SerializeField] private float jumpTime;

        public void Draw(GhostDebuggerContext context)
        {
            GhostDebuggerHelp.Info(context, "Controls the ghost's timeline. With control taken, the ghost stops moving on its own and every button sets an exact time or sample, so stepping never builds up rounding errors.");

            if (!context.HasRun)
            {
                GhostDebuggerHelp.Warning("No ghost run is loaded. Enter Play Mode in a level with a saved ghost, or pick a file in the Ghost dropdown above.");
                return;
            }

            DrawControl(context);
            EditorGUILayout.Space();
            DrawTransport(context);
            EditorGUILayout.Space();
            DrawTimeline(context);
        }

        private static void DrawControl(GhostDebuggerContext context)
        {
            EditorGUILayout.LabelField("Control", EditorStyles.boldLabel);

            if (context.Source == GhostSource.File)
            {
                GhostDebuggerHelp.Info(context, "Previewing a file. The tool owns the time and the Scene view shows where the ghost would be. Use Files > Use to play it on the scene ghost.");
                return;
            }

            if (!context.HasSceneGhost)
            {
                GhostDebuggerHelp.Warning("There is no GhostPlayer in the scene.");
                return;
            }

            var control = EditorGUILayout.ToggleLeft("Take control of the scene ghost", context.Controlling);
            if (control != context.Controlling) context.TakeControl(control);

            GhostDebuggerHelp.Info(context, control
                ? "The ghost ignores game time now. Release control to let it play normally from this point."
                : "The ghost is playing on its own. Buttons below still work, but take control to step and scrub precisely.");
        }

        private static void DrawTransport(GhostDebuggerContext context)
        {
            var playing = context.OwnsTime ? context.Playing : context.Player && context.Player.IsPlaying;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("|<", "Restart the ghost alone"), GUILayout.Width(40))) context.Restart();

                using (new EditorGUI.DisabledScope(!context.OwnsTime))
                {
                    if (GUILayout.Button(new GUIContent("<", "Step one sample back"), GUILayout.Width(40))) context.StepSample(-1);
                }

                if (GUILayout.Button(playing ? "Pause" : "Play", GUILayout.Width(80))) context.TogglePlay();

                using (new EditorGUI.DisabledScope(!context.OwnsTime))
                {
                    if (GUILayout.Button(new GUIContent(">", "Step one sample forward"), GUILayout.Width(40))) context.StepSample(1);
                }

                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(!context.OwnsTime))
                {
                    var current = Array.IndexOf(Speeds, context.Speed);
                    var picked = EditorGUILayout.Popup(current < 0 ? 3 : current, SpeedLabels, GUILayout.Width(70));
                    if (picked != current) context.SetSpeed(Speeds[picked]);
                }
            }
        }

        private void DrawTimeline(GhostDebuggerContext context)
        {
            using (new EditorGUI.DisabledScope(!context.OwnsTime))
            {
                var scrubbed = EditorGUILayout.Slider("Time", context.Time, 0f, context.Duration);
                if (!Mathf.Approximately(scrubbed, context.Time)) context.SetTime(scrubbed);

                using (new EditorGUILayout.HorizontalScope())
                {
                    jumpTime = EditorGUILayout.FloatField("Jump to time", jumpTime);
                    if (GUILayout.Button("Go", GUILayout.Width(40))) context.JumpTo(jumpTime);
                }
            }

            EditorGUILayout.LabelField($"Sample {context.CurrentIndex()} of {context.Frames.Count - 1}   ·   {context.Time:0.000} s of {context.Duration:0.000} s", EditorStyles.miniLabel);
        }
    }
}
