using System;
using Game.UI;

namespace Game.Tools.DebugCommands
{
    public class ToggleHUDCmd : DebugCommand
    {
        public bool ShowHelp { get; set; }

        public ToggleHUDCmd() : base("toggle_hud", "Toggle the HUD", "toggle_hud")
        {
            _command = ShowHelpCommand;
        }

        public bool ShowHelpCommand()
        {
            if (UIManager.Instance.TryGet(out var manager))
            {
                manager.ToggleHUD();
            }
            return true;
        }
    }
}