namespace Game.Tools.DebugCommands
{
    /// <summary>
    /// Resets the settings save to its defaults.
    /// </summary>
    public class ClearSettingsDataCmd : SecureDebugCommand
    {
        public ClearSettingsDataCmd(ConsoleSession session, IConsoleServices services)
            : base("clear-settings-data", "Deletes the settings data", "clear-settings-data", 0, session, services)
        {
        }

        protected override ConsoleResult Run(string[] args)
        {
            Services.ClearSettingsData();
            return ConsoleResult.Ok("Settings data cleared.");
        }
    }
}
