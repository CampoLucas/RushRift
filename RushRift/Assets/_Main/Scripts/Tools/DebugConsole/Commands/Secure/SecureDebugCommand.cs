using System;

namespace Game.Tools.DebugCommands
{
    /// <summary>
    /// Console command that checks login, argument count and Play Mode before it runs, and never throws.
    /// </summary>
    public abstract class SecureDebugCommand : DebugCommandBase
    {
        public virtual bool RequiresLogin => true;
        public virtual bool RequiresPlayMode => false;
        public int ArgumentCount { get; }

        protected ConsoleSession Session { get; }
        protected IConsoleServices Services { get; }

        protected SecureDebugCommand(string id, string description, string format, int argumentCount, ConsoleSession session, IConsoleServices services)
            : base(id, description, format)
        {
            ArgumentCount = argumentCount;
            Session = session;
            Services = services;
        }

        public ConsoleResult Execute(string[] args)
        {
            args ??= Array.Empty<string>();

            if (RequiresLogin && !Session.LoggedIn) return ConsoleCommandSet.NotLoggedIn;
            if (args.Length != ArgumentCount) return ConsoleResult.Error($"Usage: {Format}");
            if (RequiresPlayMode && !Services.IsPlaying) return ConsoleResult.Error($"{ID} needs Play Mode.");

            try
            {
                return Run(args);
            }
            catch (Exception e)
            {
                return ConsoleResult.Error($"{ID} failed: {e.Message}");
            }
        }

        protected abstract ConsoleResult Run(string[] args);
    }
}
