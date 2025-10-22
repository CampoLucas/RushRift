using System;
using Unity.VisualScripting;
using UnityEditor;

namespace Tools.PlayHook
{
    public class EditorAction
    {
        public string Label => _label != null ? _label() : "Empty";
        public string Tooltip => _tooltip != null ? _tooltip() : "Empty";
        public GenericMenu.MenuFunction Execute { get; }
        
        private Func<string> _label;
        private Func<string> _tooltip;

        public EditorAction(Func<string> label, Func<string> tooltip, GenericMenu.MenuFunction execute)
        {
            _label = label;
            _tooltip = tooltip;
            Execute = execute;
        }
    }
}