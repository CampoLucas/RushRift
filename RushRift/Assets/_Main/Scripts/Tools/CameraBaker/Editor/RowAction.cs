using UnityEditor;

namespace Game.Tools.CameraBaker.Editor
{
    public struct RowAction
    {
        public readonly string Name;
        public readonly float Width;
        public readonly bool Disabled;
        public readonly GenericMenu.MenuFunction Action;

        public RowAction(string name, float width, GenericMenu.MenuFunction action, bool disabled = false)
        {
            Name = name;
            Width = width;
            Action = action;
            Disabled = disabled;
        }
    }
}