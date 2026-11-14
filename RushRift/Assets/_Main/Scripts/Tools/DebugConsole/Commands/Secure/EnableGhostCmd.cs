namespace Game.Tools.DebugCommands
{
    /// <summary>
    /// Turns the ghost back on after disable-ghost.
    /// </summary>
    public class EnableGhostCmd : SecureDebugCommand
    {
        public override bool RequiresPlayMode => true;

        public EnableGhostCmd(ConsoleSession session, IConsoleServices services)
            : base("enable-ghost", "Turns the ghost on", "enable-ghost", 0, session, services)
        {
        }

        protected override ConsoleResult Run(string[] args)
        {
            var wasEnabled = Services.GhostEnabled;
            Services.SetGhostEnabled(true);
            return ConsoleResult.Ok(wasEnabled ? "Ghost was already enabled." : "Ghost enabled.");
        }
    }
}
