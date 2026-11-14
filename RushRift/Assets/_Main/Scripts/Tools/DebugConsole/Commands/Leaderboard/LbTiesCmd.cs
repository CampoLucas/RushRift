#if LEADERBOARD_CMD_ENABLED
using UnityEngine;

namespace Game.Tools.DebugCommands
{
    public class LbTiesCmd : DebugCommand<int>
    {
        public LbTiesCmd() : base("lb_ties", "Adds entries that all share the same time to the leaderboard of the current level", "lb_ties <count>")
        {
            _command = AddTies;
        }

        private bool AddTies(int count)
        {
            if (!LeaderboardCmdUtils.TryGetCurrentLevel(out var level))
            {
                Debug.LogWarning("[Leaderboard] There is no active level.");
                return false;
            }

            var sharedTime = LeaderboardCmdUtils.RandomTime();

            if (!LeaderboardCmdUtils.AddEntries(level, count, () => sharedTime))
            {
                return false;
            }

            LeaderboardCmdUtils.RefreshOnScreen();
            return true;
        }
    }
}
#endif
