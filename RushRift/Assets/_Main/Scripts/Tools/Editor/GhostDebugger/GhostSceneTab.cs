using System;
using UnityEditor;
using UnityEngine;

namespace Game.GhostDebugger
{
    /// <summary>
    /// Scene tab: settings for the ghost path drawn in the Scene view.
    /// </summary>
    [Serializable]
    public sealed class GhostSceneTab
    {
        public void Draw(GhostDebuggerContext context)
        {
            GhostDebuggerHelp.Info(context, "Draws the recorded path in the Scene view, only around the current time so long runs stay readable. Green is slow, red is the fastest part of the run.");

            context.ShowPath = EditorGUILayout.ToggleLeft("Show path", context.ShowPath);

            using (new EditorGUI.DisabledScope(!context.ShowPath))
            {
                context.PathWindow = EditorGUILayout.IntSlider("Samples each side", context.PathWindow, 5, 500);
                context.ShowMarkers = EditorGUILayout.ToggleLeft("Marker at each sample", context.ShowMarkers);
                context.SpeedColor = EditorGUILayout.ToggleLeft("Color by speed", context.SpeedColor);
            }

            if (context.HasRun) EditorGUILayout.LabelField("Fastest segment", $"{context.MaxSpeed:0.00} m/s", EditorStyles.miniLabel);
            if (GUI.changed) SceneView.RepaintAll();
        }
    }

    /// <summary>
    /// Draws the ghost path window, sample markers and current position in the Scene view without allocating.
    /// </summary>
    public static class GhostSceneDrawer
    {
        private static readonly Color Slow = new Color(0.2f, 0.9f, 0.3f);
        private static readonly Color Fast = new Color(1f, 0.25f, 0.2f);
        private static readonly Color Plain = new Color(0f, 1f, 0.6f);
        private static readonly Vector3[] Segment = new Vector3[2];

        public static void Draw(GhostDebuggerContext context)
        {
            if (!context.ShowPath || !context.HasRun) return;

            var frames = context.Frames;
            var current = context.CurrentIndex();
            var first = Mathf.Max(0, current - context.PathWindow);
            var last = Mathf.Min(frames.Count - 1, current + context.PathWindow);
            var maxSpeed = Mathf.Max(0.01f, context.MaxSpeed);
            var previousZTest = Handles.zTest;
            var previousColor = Handles.color;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

            for (var i = first + 1; i <= last; i++)
            {
                Handles.color = context.SpeedColor ? Color.Lerp(Slow, Fast, context.SegmentSpeed(i) / maxSpeed) : Plain;
                Segment[0] = frames[i - 1].position;
                Segment[1] = frames[i].position;
                Handles.DrawAAPolyLine(3f, Segment);
            }

            if (context.ShowMarkers)
            {
                Handles.color = Color.white;
                for (var i = first; i <= last; i++)
                {
                    var position = frames[i].position;
                    Handles.DotHandleCap(0, position, Quaternion.identity, HandleUtility.GetHandleSize(position) * 0.025f, EventType.Repaint);
                }
            }

            var ghost = context.Source == GhostSource.Scene && context.HasSceneGhost ? context.Player.GhostTransform.position : context.SamplePosition(context.Time);
            Handles.color = Color.yellow;
            Handles.SphereHandleCap(0, ghost, Quaternion.identity, HandleUtility.GetHandleSize(ghost) * 0.15f, EventType.Repaint);

            Handles.color = previousColor;
            Handles.zTest = previousZTest;
        }
    }
}
