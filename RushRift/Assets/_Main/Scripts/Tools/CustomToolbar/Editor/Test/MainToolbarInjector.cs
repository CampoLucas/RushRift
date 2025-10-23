using System;
using System.Linq;
using System.Reflection;
using OpenCover.Framework.Model;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tools.EditorToolbar
{
    /// <summary>
    /// Automatically scans assemblies and injects UIElements into the Unity main toolbar.
    /// </summary>
    [InitializeOnLoad]
    public class MainToolbarInjector
    {
        private static ScriptableObject _currentToolbar;
        private static bool _pendingRebuild;
        
        static MainToolbarInjector()
        {
            EditorApplication.delayCall += TryAttachToToolbar;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }
        
        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            // Unity destroys the toolbar when entering or exiting play mode,
            // so we schedule a re-attach shortly after layout rebuild.
            if (state == PlayModeStateChange.EnteredPlayMode ||
                state == PlayModeStateChange.EnteredEditMode)
            {
                _pendingRebuild = true;
                EditorApplication.delayCall += () =>
                {
                    if (_pendingRebuild)
                    {
                        _pendingRebuild = false;
                        TryAttachToToolbar();
                    }
                };
            }
        }

        private static void TryAttachToToolbar()
        {
            var toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
            var toolbars = Resources.FindObjectsOfTypeAll(toolbarType);

            if (toolbars.Length == 0)
            {
                EditorApplication.delayCall += TryAttachToToolbar;
                return;
            }
            
            var toolbar = (ScriptableObject)toolbars[0];
            if (_currentToolbar == toolbar)
                return; // Already attached to this one

            _currentToolbar = toolbar;

            var root = GetVisualTree(toolbar);
            if (root == null)
                return;
            
            // Unity names for zones inside toolbar
            var leftZone = root.Q("ToolbarZoneLeftAlign");
            var centerZone = root.Q("ToolbarZonePlayMode");
            var rightZone = root.Q("ToolbarZoneRightAlign");
            
            // Build elements
            var elements = FindAllToolbarElements();

            foreach (var info in elements)
            {
                try
                {
                    var element = info.method.Invoke(null, null) as VisualElement;
                    if (element == null)
                    {
                        Debug.LogWarning($"[MainToolbar] {info.method.DeclaringType.Name}.{info.method.Name} did not return a VisualElement.");
                        continue;
                    }

                    var zone = info.attribute.Position switch
                    {
                        ToolbarPosition.Left => leftZone,
                        ToolbarPosition.Center => centerZone,
                        ToolbarPosition.Right => rightZone,
                        _ => rightZone
                    };

                    if (zone != null)
                        zone.Add(element);
                    else
                        Debug.LogWarning($"[MainToolbar] Could not find toolbar zone for {info.attribute.Position}.");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MainToolbar] Failed to create element from {info.method.DeclaringType.Name}.{info.method.Name}: {ex}");
                }
            }
        }

        private static VisualElement GetVisualTree(ScriptableObject toolbar)
        {
            var field = toolbar.GetType().GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
            return field?.GetValue(toolbar) as VisualElement;
        }

        private static (MethodInfo method, MainToolbarElementAttribute attribute)[] FindAllToolbarElements()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => SafeGetTypes(a))
                .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                .Select(m => (method: m, attribute: m.GetCustomAttribute<MainToolbarElementAttribute>()))
                .Where(pair => pair.attribute != null)
                .ToArray();
        }

        private static Type[] SafeGetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null).ToArray();
            }
        }
    }
}