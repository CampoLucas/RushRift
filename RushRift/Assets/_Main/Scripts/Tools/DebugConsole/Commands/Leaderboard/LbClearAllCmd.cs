#if LEADERBOARD_CMD_ENABLED
using Game.Saves;

namespace Game.Tools.DebugCommands
{
    public class LbClearAllCmd : DebugCommand
    {
        public LbClearAllCmd() : base("lb_clear_all", "Removes every entry from every level of the leaderboard", "lb_clear_all")
        {
            _command = ClearAll;
        }

        private bool ClearAll()
        {
            SaveSystem.ResetLeaderBoard();
            LeaderboardCmdUtils.RefreshOnScreen();
            return true;
        }
    }
}
#endif
