using System;

namespace Game.Tools.DebugCommands
{
    public class DashHackCmd : DebugCommand
    {
        private bool _dashHack;
        
        public DashHackCmd() : base("dash_hack", "Unlimited dashes", "dash_hack")
        {
            _command = DashHackCommand;
        }
        
        private bool DashHackCommand()
        {
            _dashHack = !_dashHack;
            GlobalLevelManager.SetDashHack(_dashHack);
            return true;
        }
    }
}