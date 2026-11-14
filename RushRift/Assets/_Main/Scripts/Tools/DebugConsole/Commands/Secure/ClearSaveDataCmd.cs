namespace Game.Tools.DebugCommands
{
    /// <summary>
    /// Resets the game progress save to its defaults. SaveSystem.ResetGame also deletes the saved ghosts.
    /// </summary>
    public class ClearSaveDataCmd : SecureDebugCommand
    {
        public ClearSaveDataCmd(ConsoleSession session, IConsoleServices services)
            : base("clear-save-data", "Deletes the save and progress data, including ghosts", "clear-save-data", 0, session, services)
        {
        }

        protected override ConsoleResult Run(string[] args)
        {
            Services.ClearSaveData();
            return ConsoleResult.Ok("Save data and ghosts cleared.");
        }
    }
}
