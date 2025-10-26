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
    /// Uses ToolbarCallback to react instantly to new toolbar instances.
    /// </summary>
    [InitializeOnLoad]
    public class MainToolbarInjector
    {
        private const string LeftContainerName   = "CustomToolbarContainer_Left";
        private const string CenterContainerName = "CustomToolbarContainer_Center";
        private const string RightContainerName  = "CustomToolbarContainer_Right";
        
        private static int _lastToolbarInstanceId = -1;
        
        static MainToolbarInjector()
        {
            // React instantly when a new toolbar instance is created (edit↔play, layout reload, etc.)
            MainToolbarCallback.OnToolbarCreated += OnToolbarCreated;

            // If Unity flips modes, the toolbar instance changes; we’ll get OnToolbarCreated again.
            EditorApplication.playModeStateChanged += _ =>
            {
                _lastToolbarInstanceId = -1; // force reinject on the next created toolbar
            };
        }

        private static void OnToolbarCreated(ScriptableObject toolbar, VisualElement root)
        {
            if (toolbar == null || root == null) return;

            var id = toolbar.GetInstanceID();
            if (_lastToolbarInstanceId == id) return; // already injected for this instance

            _lastToolbarInstanceId = id;

            TryInjectInto(root);
        }
        
        private static void TryInjectInto(VisualElement root)
        {
            // Unity’s named zones
            var leftZone   = root.Q("ToolbarZoneLeftAlign");
            var centerZone = root.Q("ToolbarZonePlayMode");
            var rightZone  = root.Q("ToolbarZoneRightAlign");

            // Clean old (if any) and create fresh containers (idempotent)
            var leftContainer   = EnsureFreshContainer(leftZone,   LeftContainerName);
            var centerContainer = EnsureFreshContainer(centerZone, CenterContainerName);
            var rightContainer  = EnsureFreshContainer(rightZone,  RightContainerName);

            var elements = FindAllToolbarElements();

            foreach (var info in elements)
            {
                try
                {
                    if (info.method.Invoke(null, null) is not VisualElement element)
                    {
                        Debug.LogWarning($"[MainToolbar] {info.method.DeclaringType?.Name}.{info.method.Name} did not return a VisualElement.");
                        continue;
                    }

                    // Give a deterministic name (optional), useful for debugging
                    element.name ??= $"Toolbar_{info.method.DeclaringType?.Name}_{info.method.Name}";

                    switch (info.attribute.Position)
                    {
                        case ToolbarPosition.Left:
                            leftContainer?.Add(element);
                            break;
                        case ToolbarPosition.Center:
                            centerContainer?.Add(element);
                            break;
                        case ToolbarPosition.Right:
                            rightContainer?.Add(element);
                            break;
                        default:
                            rightContainer?.Add(element);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MainToolbar] Failed to create element from {info.method.DeclaringType?.Name}.{info.method.Name}: {ex}");
                }
            }
        }

        private static VisualElement EnsureFreshContainer(VisualElement zone, string containerName)
        {
            if (zone == null) return null;

            // Remove any previous injected container in THIS toolbar instance/zone
            zone.Q(containerName)?.RemoveFromHierarchy();

            var container = new VisualElement { name = containerName };
            container.style.flexDirection = FlexDirection.Row;
            zone.Add(container);
            return container;
        }

        // --- reflection helpers ---

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
            try { return assembly.GetTypes(); }
            catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null).ToArray(); }
        }
    }
}