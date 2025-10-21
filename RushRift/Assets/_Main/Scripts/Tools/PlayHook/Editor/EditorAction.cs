using System;
using Unity.VisualScripting;

namespace Tools.PlayHook
{
    public class EditorAction
    {
        public string Label => _label != null ? _label() : "Empty";
        public string Tooltip => _tooltip != null ? _tooltip() : "Empty";
        public Action Execute { get; }
        
        private Func<string> _label;
        private Func<string> _tooltip;

        public EditorAction(Func<string> label, Func<string> tooltip, Action execute)
        {
            _label = label;
            _tooltip = tooltip;
            Execute = execute;
        }
    }
}