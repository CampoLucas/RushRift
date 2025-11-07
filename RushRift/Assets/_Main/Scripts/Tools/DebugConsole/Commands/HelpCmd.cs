using System;

namespace Game.Tools.DebugCommands
{
    public class HelpCmd : DebugCommand
    {
        public bool ShowHelp { get; set; }

        public HelpCmd() : base("help", "Shows all the commands and their description", "help")
        {
            _command = ShowHelpCommand;
        }

        public bool ShowHelpCommand()
        {
            ShowHelp = !ShowHelp;
            return true;
        }
    }
}