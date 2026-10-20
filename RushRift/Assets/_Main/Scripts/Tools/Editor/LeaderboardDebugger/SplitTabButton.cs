using System;
using UnityEditor;
using UnityEngine;

namespace Game.LeaderboardDebugger
{
    /// <summary>
    /// Tab button made of a main toggle and a dropdown arrow toggle that share one style and highlight together.
    /// </summary>
    public static class SplitTabButton
    {
        private const float ArrowWidth = 22f;

        public static void Draw(string label, bool active, float height, Action onSelectMain, Action<Rect> onOpenMenu)
        {
            if (GUILayout.Toggle(active, label, EditorStyles.miniButtonMid, GUILayout.Height(height), GUILayout.ExpandWidth(true)) != active) onSelectMain();

            var arrow = EditorGUIUtility.IconContent("icon dropdown");
            var arrowRect = GUILayoutUtility.GetRect(arrow, EditorStyles.miniButtonRight, GUILayout.Width(ArrowWidth), GUILayout.Height(height));
            if (GUI.Toggle(arrowRect, active, arrow, EditorStyles.miniButtonRight) != active) onOpenMenu(arrowRect);
        }
    }
}
