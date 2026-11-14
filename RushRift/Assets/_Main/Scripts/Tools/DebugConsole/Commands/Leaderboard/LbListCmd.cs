#if LEADERBOARD_CMD_ENABLED
using System.Text;
using Game.Saves;
using UnityEngine;

namespace Game.Tools.DebugCommands
{
    public class LbListCmd : DebugCommand
    {
        public LbListCmd() : base("lb_list", "Logs the sorted entries of the leaderboard of the current level", "lb_list")
        {
            _command = List;
        }

        private bool List()
        {
            if (!LeaderboardCmdUtils.TryGetCurrentLevel(out var level))
            {
                Debug.LogWarning("[Leaderboard] There is no active level.");
                return false;
            }

            var data = SaveSystem.LoadLeaderBoard();

            if (!data.Score.leaderBoardList.ContainsKey(level))
            {
                Debug.Log($"[Leaderboard] Level {level} has no entries.");
                return true;
            }

            var builder = new StringBuilder($"[Leaderboard] Level {level}\n");
            var entries = data.GetLevelLeaderBoard(level);

            for (var i = 0; i < entries.Count; i++)
            {
                builder.AppendLine($"#{i + 1} {entries[i].name} ({entries[i].id}) {entries[i].timescore}");
            }

            Debug.Log(builder.ToString());
            return true;
        }
    }
}
#endif
