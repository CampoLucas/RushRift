using System;

namespace Tools.EditorToolbar
{
	public enum ToolbarPosition
	{
		Left,
		Center,
		Right
	}

	/// <summary>
	/// Attribute used to mark methods or classes that create VisualElements for the main toolbar.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class MainToolbarElementAttribute : Attribute
    {
        public ToolbarPosition Position { get; }

        public MainToolbarElementAttribute(ToolbarPosition position)
        {
	        Position = position;
        }
    }
}