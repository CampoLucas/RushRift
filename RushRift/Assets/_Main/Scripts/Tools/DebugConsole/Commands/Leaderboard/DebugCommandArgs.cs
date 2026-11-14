#if LEADERBOARD_CMD_ENABLED
using System;

namespace Game.Tools.DebugCommands
{
    public class DebugCommandArgs : DebugCommandBase
    {
        protected Func<string[], bool> _command;

        public DebugCommandArgs(string id, string description, string format)
            : base(id, description, format)
        {

        }

        public bool Do(string[] args)
        {
            return _command.Invoke(args);
        }

        public override void Dispose()
        {
            _command = null;
        }
    }
}
#endif
