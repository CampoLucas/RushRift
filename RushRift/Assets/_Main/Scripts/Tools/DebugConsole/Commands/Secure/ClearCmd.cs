namespace Game.Tools.DebugCommands
{
    /// <summary>
    /// Clears the command history of the console.
    /// </summary>
    public class ClearCmd : SecureDebugCommand
    {
        public ClearCmd(ConsoleSession session, IConsoleServices services)
            : base("clear", "Clears the command history", "clear", 0, session, services)
        {
        }

        protected override ConsoleResult Run(string[] args)
        {
            Session.ClearHistory();
            return ConsoleResult.Ok("Command history cleared.");
        }
    }
}
