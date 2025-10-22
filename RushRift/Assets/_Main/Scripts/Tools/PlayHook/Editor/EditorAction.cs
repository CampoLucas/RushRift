using System;
using Unity.VisualScripting;
using UnityEditor;

namespace Tools.PlayHook
{
    public class EditorAction
    {
        public string Label { get; }
        public string Tooltip { get; }
        public GenericMenu.MenuFunction Execute { get; }

        public EditorAction(string label, string tooltip, GenericMenu.MenuFunction execute)
        {
            Label = label;
            Tooltip = tooltip;
            Execute = execute;
        }
    }
}