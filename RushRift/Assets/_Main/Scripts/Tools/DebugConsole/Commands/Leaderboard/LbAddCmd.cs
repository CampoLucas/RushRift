#if LEADERBOARD_CMD_ENABLED
using System.Globalization;
using UnityEngine;

namespace Game.Tools.DebugCommands
{
    public class LbAddCmd : DebugCommandArgs
    {
        private const string FailMessage = "[Leaderboard] It cannot be added";

        public LbAddCmd() : base("lb_add",
            "Adds an entry. The name is also the player id, so repeating a name overwrites it. Time defaults to the current level time and level id to the current level.",
            "lb_add <name> [time] [level-id]")
        {
            _command = Add;
        }

        private bool Add(string[] args)
        {
            if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
            {
                Debug.LogWarning($"{FailMessage}: the name cannot be empty.");
                return false;
            }

            var time = GlobalLevelManager.CurrentTime;
            if (args.Length > 1 && !float.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out time))
            {
                Debug.LogWarning($"{FailMessage}: '{args[1]}' is not a valid time.");
                return false;
            }

            var level = -1;
            if (args.Length > 2)
            {
                if (!int.TryParse(args[2], out level))
                {
                    Debug.LogWarning($"{FailMessage}: '{args[2]}' is not a valid level id.");
                    return false;
                }
            }
            else if (!LeaderboardCmdUtils.TryGetCurrentLevel(out level))
            {
                Debug.LogWarning($"{FailMessage}: there is no active level.");
                return false;
            }

            LeaderboardCmdUtils.AddEntry(level, args[0], args[0], time);
            LeaderboardCmdUtils.RefreshOnScreen();
            Debug.Log($"[Leaderboard] Added '{args[0]}' with time {time} to level {level}.");
            return true;
        }
    }
}
#endif
