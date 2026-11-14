namespace Game.Tools.DebugCommands
{
    /// <summary>
    /// Hides and stops the ghost, including ghosts in levels loaded afterwards.
    /// </summary>
    public class DisableGhostCmd : SecureDebugCommand
    {
        public override bool RequiresPlayMode => true;

        public DisableGhostCmd(ConsoleSession session, IConsoleServices services)
            : base("disable-ghost", "Turns the ghost off", "disable-ghost", 0, session, services)
        {
        }

        protected override ConsoleResult Run(string[] args)
        {
            var wasEnabled = Services.GhostEnabled;
            Services.SetGhostEnabled(false);
            return ConsoleResult.Ok(wasEnabled ? "Ghost disabled." : "Ghost was already disabled.");
        }
    }
}
