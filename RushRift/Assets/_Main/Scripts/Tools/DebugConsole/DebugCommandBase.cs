using System;

namespace Game.Tools.DebugCommands
{
    public class DebugCommandBase : IDisposable
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

        public virtual void Dispose()
        {
            
        }
    }

    public class DebugCommand : DebugCommandBase
    {
        private Func<bool> _command;
        
        public DebugCommand(string id, string description, string format, Func<bool> command) 
            : base(id, description, format)
        {
            _command = command;
        }

        public bool Do()
        {
            return _command.Invoke();
        }

        public override void Dispose()
        {
            _command = null;
        }
    }

    public class DebugCommand<T> : DebugCommandBase
    {
        private Func<T, bool> _command;
        
        public DebugCommand(string id, string description, string format, Func<T, bool> command) 
            : base(id, description, format)
        {
            _command = command;
        }

        public bool Do(T arg)
        {
            return _command.Invoke(arg);
        }

        public override void Dispose()
        {
            _command = null;
        }
    }
    
    public class DebugCommand<T1, T2> : DebugCommandBase
    {
        private Func<T1, T2, bool> _command;
        
        public DebugCommand(string id, string description, string format, Func<T1, T2, bool> command) 
            : base(id, description, format)
        {
            _command = command;
        }

        public bool Do(T1 arg1, T2 arg2)
        {
            return _command.Invoke(arg1, arg2);
        }

        public override void Dispose()
        {
            _command = null;
        }
    }
}