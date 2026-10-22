#if LEADERBOARD_CMD_ENABLED
using UnityEngine;

namespace Game.Tools.DebugCommands
{
    public class LbFillCmd : DebugCommand<int>
    {
        public LbFillCmd() : base("lb_fill", "Adds random entries to the leaderboard of the current level", "lb_fill <count>")
        {
            _command = Fill;
        }

        private bool Fill(int count)
        {
            if (!LeaderboardCmdUtils.TryGetCurrentLevel(out var level))
            {
                Debug.LogWarning("[Leaderboard] There is no active level.");
                return false;
            }

            if (!LeaderboardCmdUtils.AddEntries(level, count, LeaderboardCmdUtils.RandomTime))
            {
                return false;
            }

            LeaderboardCmdUtils.RefreshOnScreen();
            return true;
        }
    }
}
#endif
