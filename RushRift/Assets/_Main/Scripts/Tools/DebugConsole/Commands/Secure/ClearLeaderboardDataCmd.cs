namespace Game.Tools.DebugCommands
{
    /// <summary>
    /// Removes every leaderboard entry of every level.
    /// </summary>
    public class ClearLeaderboardDataCmd : SecureDebugCommand
    {
        public ClearLeaderboardDataCmd(ConsoleSession session, IConsoleServices services)
            : base("clear-leaderboard-data", "Deletes the leaderboard data", "clear-leaderboard-data", 0, session, services)
        {
        }

        protected override ConsoleResult Run(string[] args)
        {
            Services.ClearLeaderboardData();
            return ConsoleResult.Ok("Leaderboard data cleared.");
        }
    }
}
