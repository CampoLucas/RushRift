using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Tools.PlayHook.Utils
{
    public static class DefineSymbolUtility
    {
        public static void ToggleDefine(string define)
        {
            var group = EditorUserBuildSettings.selectedBuildTargetGroup;
            var hasDefine = HasDefine(define, group, out var defines);
            
            // Show confirmation dialog
            var title = hasDefine ? $"Remove {define} Define Symbol" : $"Add {define} Define Symbol";
            var message = hasDefine
                ? $"Are you sure you want to remove '{define}'?\n\nThis will trigger a full recompilation."
                : $"Are you sure you want to add '{define}'?\n\nThis will trigger a full recompilation.";
            
            if (!EditorUtility.DisplayDialog(title, message, "Yes", "Cancel"))
                return; // user canceled

            if (hasDefine)
            {
                defines.Remove(define);
            }
            else
            {
                defines.Add(define);
            }

            var newDefines = string.Join(";", defines);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, newDefines);
            
            Debug.Log($"[DefineSymbolUtility] {(hasDefine ? "Removed" : "Added")} '{define}'.");
        }

        public static bool HasDefine(string define)
        {
            return HasDefine(define, EditorUserBuildSettings.selectedBuildTargetGroup, out var defines);
        }

        private static bool HasDefine(string define, BuildTargetGroup group, out List<string> defines)
        {
            defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group)
                .Split(';')
                .Where(d => !string.IsNullOrEmpty(d))
                .ToList();

            return defines.Contains(define);
        }
    }
}