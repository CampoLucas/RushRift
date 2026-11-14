namespace Game.Tools.DebugCommands
{
    /// <summary>
    /// Unlocks every console command for the session when the password matches the console config.
    /// </summary>
    public class LoginCmd : SecureDebugCommand
    {
        public override bool RequiresLogin => false;

        public LoginCmd(ConsoleSession session, IConsoleServices services)
            : base(ConsoleCommandSet.LoginId, "Unlocks the console commands", "login <password>", 1, session, services)
        {
        }

        protected override ConsoleResult Run(string[] args)
        {
            if (!Session.HasPassword) return ConsoleResult.Error("No console password is set in the console config.");

            return Session.TryLogin(args[0])
                ? ConsoleResult.Ok("Logged in. Console commands unlocked.")
                : ConsoleResult.Error("Wrong password.");
        }
    }
}
