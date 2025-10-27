using System;

namespace Game.Tools.DebugCommands
{
    public class DebugCommandBase
    {
        public string ID { get; private set; }
        public string Description { get; private set; }
        public string Format { get; private set; }

        public DebugCommandBase(string id, string description, string format)
        {
            ID = id;
            Description = description;
            Format = format;
        }
    }

    public class DebugCommand : DebugCommandBase
    {
        private Action _command;
        
        public DebugCommand(string id, string description, string format, Action command) 
            : base(id, description, format)
        {
            _command = command;
        }

        public void Do()
        {
            _command.Invoke();
        }
    }
}