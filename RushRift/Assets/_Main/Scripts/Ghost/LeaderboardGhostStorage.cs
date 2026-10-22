using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Saves;
using UnityEngine;

namespace _Main.Scripts.Ghost
{
    public static class LeaderboardGhostStorage
    {
        public static string Save(GhostRecorder.GhostRunData run)
        {
            string path = GetPath(run.leaderboardLevelId, run.leaderboardPlayerId);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporaryPath = path + ".tmp";
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(run));
            if (File.Exists(path)) File.Replace(temporaryPath, path, null);
            else File.Move(temporaryPath, path);
            return path;
        }

        public static bool TryLoadBestForLevel(int levelId, string sceneName,
            out GhostRecorder.GhostRunData run, out string path)
        {
            run = null;
            path = "";
            var leaderboard = SaveSystem.LoadLeaderBoard();
            if (!leaderboard.Score.leaderBoardList.TryGetValue(levelId, out var entries) || entries.Count == 0)
                return false;

            // Use the same ordering as the leaderboard UI, including its tie ordering.
            var best = leaderboard.GetLevelLeaderBoard(levelId)[0];
            return TryLoad(levelId, sceneName, best, out run, out path);
        }

        public static bool TryLoad(int levelId, string sceneName, Score score,
            out GhostRecorder.GhostRunData run, out string path)
        {
            run = null;
            path = "";
            if (string.IsNullOrEmpty(score.id) || !IsValidTime(score.timescore)) return false;
            path = GetPath(levelId, score.id);
            try
            {
                if (!File.Exists(path)) return false;
                var candidate = JsonUtility.FromJson<GhostRecorder.GhostRunData>(File.ReadAllText(path));
                if (candidate == null || candidate.leaderboardLevelId != levelId ||
                    candidate.leaderboardPlayerId != score.id || candidate.levelName != sceneName ||
                    !candidate.durationSeconds.Equals(score.timescore) || candidate.frames == null ||
                    candidate.frames.Count < 2) return false;

                float previousTime = -1f;
                foreach (var frame in candidate.frames)
                {
                    if (float.IsNaN(frame.time) || float.IsInfinity(frame.time) ||
                        frame.time < 0f || frame.time < previousTime || frame.time > candidate.durationSeconds)
                        return false;
                    previousTime = frame.time;
                }
                run = candidate;
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
            {
                return false;
            }
        }

        public static bool IsValidTime(float time) => time > 0f && !float.IsNaN(time) && !float.IsInfinity(time);

        private static string GetPath(int levelId, string playerId)
        {
            // Player IDs may contain characters that cannot be used in file names.
            using var hash = SHA256.Create();
            string key = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(playerId))).Replace("-", "");
            return Path.Combine(Application.persistentDataPath, "ghosts", "leaderboard", $"level_{levelId}_{key}.ghost.json");
        }
    }
}
