namespace Game.Tools.DebugCommands
{
    /// <summary>
    /// Locks every console command again and clears the command history.
    /// </summary>
    public class LogoutCmd : SecureDebugCommand
    {
        public LogoutCmd(ConsoleSession session, IConsoleServices services)
            : base("logout", "Locks the console commands", "logout", 0, session, services)
        {
        }

        protected override ConsoleResult Run(string[] args)
        {
            Session.Logout();
            Session.ClearHistory();
            return ConsoleResult.Ok("Logged out. Console commands locked.");
        }
    }
}
