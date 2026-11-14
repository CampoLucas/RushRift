#if LEADERBOARD_CMD_ENABLED
using System;
using System.Collections.Generic;
using Game.Saves;
using UnityEngine;

namespace Game.Tools.DebugCommands
{
    public static class LeaderboardCmdUtils
    {
        private static readonly System.Random Random = new();

        private static readonly string[] FirstParts =
        {
            "Rift", "Dash", "Blink", "Neon", "Turbo", "Echo", "Pixel", "Nova", "Vortex", "Zero"
        };

        private static readonly string[] SecondParts =
        {
            "Runner", "Pilot", "Fox", "Wolf", "Storm", "Spark", "Blade", "Rider", "Hawk", "Bolt"
        };

        public static IEnumerable<object> CreateCommands()
        {
            yield return new LbClearAllCmd();
            yield return new LbClearLevelCmd();
            yield return new LbFillCmd();
            yield return new LbTiesCmd();
            yield return new LbAddCmd();
            yield return new LbListCmd();
        }

        public static bool TryGetCurrentLevel(out int level)
        {
            level = -1;

            if (!GlobalLevelManager.CurrentLevel.TryGet(out var config))
            {
                return false;
            }

            level = config.LevelID;
            return true;
        }

        public static string RandomName()
        {
            return FirstParts[Random.Next(FirstParts.Length)] + SecondParts[Random.Next(SecondParts.Length)] + Random.Next(0, 100);
        }

        public static float RandomTime()
        {
            return (float)Math.Round(20.0 + Random.NextDouble() * 280.0, 3);
        }

        public static bool AddEntry(int level, string name, string id, float time)
        {
            var data = SaveSystem.LoadLeaderBoard();
            data.AddTimeToLevel(level, new PlayerID { name = name, id = id }, time);
            SaveSystem.SaveLeaderBoard(data);
            return true;
        }

        public static bool AddEntries(int level, int count, Func<float> timeSource)
        {
            if (count <= 0)
            {
                Debug.LogWarning("[Leaderboard] The count must be greater than 0.");
                return false;
            }

            var data = SaveSystem.LoadLeaderBoard();

            for (var i = 0; i < count; i++)
            {
                data.AddTimeToLevel(level, new PlayerID { name = RandomName(), id = Guid.NewGuid().ToString() }, timeSource());
            }

            SaveSystem.SaveLeaderBoard(data);
            return true;
        }

        public static void RefreshOnScreen()
        {
            foreach (var presenter in UnityEngine.Object.FindObjectsOfType<LeaderboardPresenter>())
            {
                try
                {
                    presenter.GetScoreData();
                }
                catch (KeyNotFoundException)
                {
                }
            }
        }
    }
}
#endif
