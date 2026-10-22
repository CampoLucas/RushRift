#if GHOST_LEADERBOARD_OBSOLETE
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using _Main.Scripts.Ghost;
using Game.Saves;
using NUnit.Framework;
using UnityEngine;

namespace Game.LeaderboardDebugger.Tests
{
    public class GhostLeaderboardTests
    {
        private const int Level = 123456;
        private readonly List<string> paths = new();
        private SaveBackup backup;
        private GameObject recorderObject;

        [SetUp]
        public void SetUp() => backup = LeaderboardDebuggerService.Capture();

        [TearDown]
        public void TearDown()
        {
            if (recorderObject) UnityEngine.Object.DestroyImmediate(recorderObject);
            foreach (string path in paths) if (File.Exists(path)) File.Delete(path);
            paths.Clear();
            backup.Restore();
        }

        private string Record(string id, float time)
        {
            var run = new GhostRecorder.GhostRunData
            {
                levelName = "R1L1", leaderboardLevelId = Level, leaderboardPlayerId = id,
                durationSeconds = time,
                frames = new List<GhostRecorder.GhostFrame>
                {
                    new() { time = 0f, rotation = Quaternion.identity },
                    new() { time = time, position = Vector3.right, rotation = Quaternion.identity }
                }
            };
            string path = LeaderboardGhostStorage.Save(run);
            paths.Add(path);
            return path;
        }

        private void SetLeaderboard(params Score[] scores)
        {
            var data = new LeaderBoardData();
            data.Score.leaderBoardList[Level] = new Dictionary<string, Score>();
            foreach (var score in scores) data.Score.leaderBoardList[Level][score.id] = score;
            SaveSystem.SaveLeaderBoard(data);
        }

        [Test]
        public void LoadsFirstPlaceRecordingRegardlessOfSaveOrder()
        {
            string winner = Guid.NewGuid().ToString(), other = Guid.NewGuid().ToString();
            Record(winner, 10f);
            Record(other, 20f);
            SetLeaderboard(new Score { id = other, timescore = 20f }, new Score { id = winner, timescore = 10f });
            Assert.IsTrue(LeaderboardGhostStorage.TryLoadBestForLevel(Level, "R1L1", out var run, out _));
            Assert.AreEqual(winner, run.leaderboardPlayerId);
            Assert.AreEqual(10f, run.durationSeconds);
        }

        [Test]
        public void MissingWinnerRecordingDoesNotFallBackToAnotherPlayer()
        {
            string other = Guid.NewGuid().ToString();
            Record(other, 20f);
            SetLeaderboard(new Score { id = Guid.NewGuid().ToString(), timescore = 5f }, new Score { id = other, timescore = 20f });
            Assert.IsFalse(LeaderboardGhostStorage.TryLoadBestForLevel(Level, "R1L1", out _, out _));
        }

        [Test]
        public void ChangedScoreOrSceneRejectsStaleRecording()
        {
            string id = Guid.NewGuid().ToString();
            Record(id, 10f);
            Assert.IsFalse(LeaderboardGhostStorage.TryLoad(Level, "R1L1", new Score { id = id, timescore = 9f }, out _, out _));
            Assert.IsFalse(LeaderboardGhostStorage.TryLoad(Level, "R2L1", new Score { id = id, timescore = 10f }, out _, out _));
            Assert.IsFalse(LeaderboardGhostStorage.TryLoad(Level + 1, "R1L1", new Score { id = id, timescore = 10f }, out _, out _));
        }

        [Test]
        public void DeletedLeaderboardHidesExistingRecording()
        {
            Record(Guid.NewGuid().ToString(), 10f);
            SaveSystem.SaveLeaderBoard(new LeaderBoardData());
            Assert.IsFalse(LeaderboardGhostStorage.TryLoadBestForLevel(Level, "R1L1", out _, out _));
        }

        [Test]
        public void PlayerIdsWithPathCharactersRoundTripSafely()
        {
            string id = "../player:/" + Guid.NewGuid();
            string path = Record(id, 10f);
            Assert.AreEqual("leaderboard", new DirectoryInfo(Path.GetDirectoryName(path)).Name);
            Assert.IsTrue(LeaderboardGhostStorage.TryLoad(Level, "R1L1", new Score { id = id, timescore = 10f }, out var run, out _));
            Assert.AreEqual(id, run.leaderboardPlayerId);
        }

        [Test]
        public void CorruptRecordingDoesNotPreventLevelLoading()
        {
            string id = Guid.NewGuid().ToString();
            File.WriteAllText(Record(id, 10f), "not JSON");
            Assert.IsFalse(LeaderboardGhostStorage.TryLoad(Level, "R1L1", new Score { id = id, timescore = 10f }, out _, out _));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void CapturesOfficialTimeBeforeOrAfterRecorderStops(bool stillRecording)
        {
            string id = Guid.NewGuid().ToString();
            recorderObject = new GameObject("LeaderboardGhostTest");
            var recorder = recorderObject.AddComponent<GhostRecorder>();
            var run = new GhostRecorder.GhostRunData
            {
                levelName = "R1L1", durationSeconds = 1f,
                frames = new List<GhostRecorder.GhostFrame>
                {
                    new() { time = 0f, rotation = Quaternion.identity },
                    new() { time = 0.5f, position = Vector3.right, rotation = Quaternion.identity },
                    new() { time = 1f, position = Vector3.right * 2f, rotation = Quaternion.identity }
                }
            };
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(GhostRecorder).GetField("currentRun", flags).SetValue(recorder, run);
            typeof(GhostRecorder).GetField("recordedLevelId", flags).SetValue(recorder, Level);
            typeof(GhostRecorder).GetField("isRecording", flags).SetValue(recorder, stillRecording);
            Assert.IsTrue(GhostRecorder.SaveLeaderboardGhost(Level, id, 1.2f));
            Assert.IsTrue(LeaderboardGhostStorage.TryLoad(Level, "R1L1", new Score { id = id, timescore = 1.2f }, out var replay, out var path));
            paths.Add(path);
            Assert.AreEqual(0.6f, replay.frames[1].time, 0.00001f);
            Assert.AreEqual(1.2f, replay.frames[replay.frames.Count - 1].time);
            Assert.IsFalse(GhostRecorder.SaveLeaderboardGhost(Level, id, 1.2f), "A run must only be attached once.");
        }

        [Test]
        public void ChangingFirstPlaceSelectsItsOwnRecording()
        {
            string previous = Guid.NewGuid().ToString(), next = Guid.NewGuid().ToString();
            Record(previous, 10f);
            Record(next, 9f);
            SetLeaderboard(new Score { id = previous, timescore = 10f });
            Assert.IsTrue(LeaderboardGhostStorage.TryLoadBestForLevel(Level, "R1L1", out var run, out _));
            Assert.AreEqual(previous, run.leaderboardPlayerId);
            SetLeaderboard(new Score { id = previous, timescore = 10f }, new Score { id = next, timescore = 9f });
            Assert.IsTrue(LeaderboardGhostStorage.TryLoadBestForLevel(Level, "R1L1", out run, out _));
            Assert.AreEqual(next, run.leaderboardPlayerId);
        }
    }
}
#else
using NUnit.Framework;

namespace Game.LeaderboardDebugger.Tests
{
    /// <summary>
    /// Obsolete: leaderboard ghosts were removed. Each player now races their own best ghost.
    /// </summary>
    public class GhostLeaderboardTests
    {
        [Test, Ignore("Obsolete: leaderboard ghosts were removed.")]
        public void LeaderboardGhosts() { }
    }
}
#endif
