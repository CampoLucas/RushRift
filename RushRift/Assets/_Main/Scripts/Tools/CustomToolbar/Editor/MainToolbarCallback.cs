using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tools.EditorToolbar
{
    public static class MainToolbarCallback
    {
        public static readonly Type ToolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
        private static ScriptableObject _currentToolbar;

        /// <summary>Fires ONCE per toolbar instance, right after we’re attached to it.</summary>
        public static event Action<ScriptableObject, VisualElement> OnToolbarCreated;

        static MainToolbarCallback()
        {
            EditorApplication.update += WaitForToolbar;
            EditorApplication.playModeStateChanged += _ =>
            {
                // Toolbar object is about to be destroyed/recreated -> start watching again
                _currentToolbar = null;
                EditorApplication.update -= WaitForToolbar;
                EditorApplication.update += WaitForToolbar;
            };
        }
        
        private static void WaitForToolbar()
        {
            if (_currentToolbar != null) 
                return;
            
            var toolbars = Resources.FindObjectsOfTypeAll(ToolbarType);
            if (toolbars == null || toolbars.Length == 0) 
                return;

            _currentToolbar = (ScriptableObject)toolbars[0];
            
            // Stop polling until next playmode/layout cycle
            EditorApplication.update -= WaitForToolbar;

            var rootVE = GetRoot(_currentToolbar);
            if (rootVE == null)
            {
                return;
            }
            
            // Fire exactly once per instance
            OnToolbarCreated?.Invoke(_currentToolbar, rootVE);
            
            // Also hook IMGUI repaint if you want, but injection should be done above already
            var imgui = rootVE.Q<IMGUIContainer>();
            if (imgui == null) return;
            var f = typeof(IMGUIContainer).GetField("m_OnGUIHandler", BindingFlags.NonPublic | BindingFlags.Instance);
            var h = (Action)f?.GetValue(imgui);
            // keep lightweight — don’t reinject here; just keep for debugging if needed
            f?.SetValue(imgui, h);
        }
        
        private static VisualElement GetRoot(ScriptableObject toolbar)
        {
            var field = ToolbarType.GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
            return field?.GetValue(toolbar) as VisualElement;
        }

    }
}