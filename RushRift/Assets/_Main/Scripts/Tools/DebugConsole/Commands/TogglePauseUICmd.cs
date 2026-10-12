using System;
using Game.UI;

namespace Game.Tools.DebugCommands
{
    public class TogglePauseUICmd : DebugCommand
    {
        public bool ShowHelp { get; set; }

        public TogglePauseUICmd() : base("toggle_pause_ui", "Toggle the Pause UI", "toggle_pause_ui")
        {
            _command = ShowHelpCommand;
        }

        public bool ShowHelpCommand()
        {
            if (UIManager.Instance.TryGet(out var manager))
            {
                manager.TogglePauseUI();
            }
            
            return true;
        }
    }
}