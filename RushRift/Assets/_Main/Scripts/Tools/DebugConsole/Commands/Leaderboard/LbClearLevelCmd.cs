#if LEADERBOARD_CMD_ENABLED
using Game.Saves;
using UnityEngine;

namespace Game.Tools.DebugCommands
{
    public class LbClearLevelCmd : DebugCommand
    {
        public LbClearLevelCmd() : base("lb_clear_level", "Removes every entry from the leaderboard of the current level", "lb_clear_level")
        {
            _command = ClearLevel;
        }

        private bool ClearLevel()
        {
            if (!LeaderboardCmdUtils.TryGetCurrentLevel(out var level))
            {
                Debug.LogWarning("[Leaderboard] There is no active level.");
                return false;
            }

            var data = SaveSystem.LoadLeaderBoard();
            data.Score.leaderBoardList.Remove(level);
            SaveSystem.SaveLeaderBoard(data);
            LeaderboardCmdUtils.RefreshOnScreen();
            return true;
        }
    }
}
#endif
