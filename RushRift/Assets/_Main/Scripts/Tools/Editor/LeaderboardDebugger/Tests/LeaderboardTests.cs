using System;
using System.Collections.Generic;
using System.Linq;
using Game.Saves;
using NUnit.Framework;

namespace Game.LeaderboardDebugger.Tests
{
    public class LeaderboardTests
    {
        private const int Level = 1;

        private SaveBackup backup;

        [SetUp]
        public void SetUp()
        {
            backup = LeaderboardDebuggerService.Capture();
        }

        [TearDown]
        public void TearDown()
        {
            backup.Restore();
        }

        private static List<Score> Entries(params float[] times)
        {
            return times.Select((time, index) => new Score { name = $"Player{index}", id = $"id-{index}", timescore = time }).ToList();
        }

        private static void AssertSameAfterReload(LeaderBoardData data)
        {
            LeaderboardDebuggerService.Save(data);
            var problem = LeaderboardDebuggerService.Compare(data, LeaderboardDebuggerService.Load());
            Assert.IsNull(problem, problem);
        }

        [Test]
        public void Sorting_PutsLowestTimeFirst()
        {
            var data = LeaderboardDebuggerService.BuildData(Level, Entries(30f, 10f, 20f));

            var times = data.GetLevelLeaderBoard(Level).Select(score => score.timescore).ToArray();

            CollectionAssert.AreEqual(new[] { 10f, 20f, 30f }, times);
        }

        [Test]
        public void Sorting_MatchesLocallyComputedOrderForEveryDistribution()
        {
            foreach (ScoreDistribution distribution in Enum.GetValues(typeof(ScoreDistribution)))
            {
                var entries = LeaderboardGenerator.Generate(250, distribution, 20f, 300f, new Random(21));
                var data = LeaderboardDebuggerService.BuildData(Level, entries);

                var problem = LeaderboardDebuggerService.CompareOrder(data.GetLevelLeaderBoard(Level), entries);

                Assert.IsNull(problem, $"{distribution}: {problem}");
            }
        }

        [Test]
        public void Sorting_SurvivesSaveAndReload()
        {
            var entries = LeaderboardGenerator.Generate(100, ScoreDistribution.Uniform, 20f, 300f, new Random(5));
            LeaderboardDebuggerService.Save(LeaderboardDebuggerService.BuildData(Level, entries));

            var reloaded = LeaderboardDebuggerService.Load();

            var problem = LeaderboardDebuggerService.CompareOrder(reloaded.GetLevelLeaderBoard(Level), entries);
            Assert.IsNull(problem, problem);
        }

        [Test]
        public void Ties_KeepEveryEntry()
        {
            var random = new Random(9);
            var entries = LeaderboardGenerator.Generate(10, ScoreDistribution.Uniform, 20f, 100f, random);
            entries.AddRange(LeaderboardGenerator.CreateTies(8, 50f, random));
            var data = LeaderboardDebuggerService.BuildData(Level, entries);

            var sorted = data.GetLevelLeaderBoard(Level);

            Assert.AreEqual(entries.Count, sorted.Count);
            CollectionAssert.AreEquivalent(entries.Select(score => score.id), sorted.Select(score => score.id));
        }

        [Test]
        public void Ties_AreGroupedBetweenFasterAndSlowerEntries()
        {
            var data = LeaderboardDebuggerService.BuildData(Level, Entries(40f, 50f, 50f, 50f, 60f));

            var times = data.GetLevelLeaderBoard(Level).Select(score => score.timescore).ToArray();

            CollectionAssert.AreEqual(new[] { 40f, 50f, 50f, 50f, 60f }, times);
        }

        [Test]
        public void Ties_OrderIsConsistentBetweenReads()
        {
            var entries = LeaderboardGenerator.CreateTies(25, 50f, new Random(2));
            var data = LeaderboardDebuggerService.BuildData(Level, entries);

            var first = data.GetLevelLeaderBoard(Level).Select(score => score.id).ToList();
            var second = data.GetLevelLeaderBoard(Level).Select(score => score.id).ToList();

            CollectionAssert.AreEqual(first, second);
        }

        [Test]
        public void PersonalBest_SameIdKeepsOneEntry()
        {
            var data = new LeaderBoardData();
            LeaderboardDebuggerService.AddEntry(data, Level, "Name", "same-id", 30f);
            LeaderboardDebuggerService.AddEntry(data, Level, "Name", "same-id", 20f);

            var sorted = data.GetLevelLeaderBoard(Level);

            Assert.AreEqual(1, sorted.Count);
            Assert.AreEqual(20f, sorted[0].timescore);
        }

        [Test]
        public void PersonalBest_AddTimeToLevelOverwritesWithoutComparing()
        {
            var data = new LeaderBoardData();
            LeaderboardDebuggerService.AddEntry(data, Level, "Name", "same-id", 20f);
            LeaderboardDebuggerService.AddEntry(data, Level, "Name", "same-id", 30f);

            Assert.AreEqual(30f, data.GetLevelLeaderBoard(Level)[0].timescore);
        }

        [Test]
        public void PersonalBest_CheckBestTimeAcceptsFirstTime()
        {
            var save = new SaveData();

            save.CheckBestTime(Level, 20f, out _, out var best, out var newRecord);

            Assert.IsTrue(newRecord);
            Assert.AreEqual(20f, best);
        }

        [Test]
        public void PersonalBest_CheckBestTimeRejectsWorseTime()
        {
            var save = new SaveData();
            save.SetNewBestTime(Level, 20f);

            save.CheckBestTime(Level, 25f, out var previous, out var best, out var newRecord);

            Assert.IsFalse(newRecord);
            Assert.AreEqual(20f, previous);
            Assert.AreEqual(20f, best);
        }

        [Test]
        public void PersonalBest_CheckBestTimeAcceptsBetterTime()
        {
            var save = new SaveData();
            save.SetNewBestTime(Level, 20f);

            save.CheckBestTime(Level, 15f, out _, out var best, out var newRecord);

            Assert.IsTrue(newRecord);
            Assert.AreEqual(15f, best);
        }

        [Test]
        public void PersonalBest_CheckBestTimeRejectsEqualTime()
        {
            var save = new SaveData();
            save.SetNewBestTime(Level, 20f);

            save.CheckBestTime(Level, 20f, out _, out _, out var newRecord);

            Assert.IsFalse(newRecord);
        }

        [Test]
        public void Levels_KeepSeparateEntries()
        {
            var data = new LeaderBoardData();
            LeaderboardDebuggerService.AddEntry(data, 1, "A", "id-a", 10f);
            LeaderboardDebuggerService.AddEntry(data, 2, "A", "id-a", 99f);

            Assert.AreEqual(10f, data.GetLevelLeaderBoard(1)[0].timescore);
            Assert.AreEqual(99f, data.GetLevelLeaderBoard(2)[0].timescore);
        }

        [Test]
        public void SameNameWithDifferentIdsKeepsBothEntries()
        {
            var data = new LeaderBoardData();
            LeaderboardDebuggerService.AddEntry(data, Level, "Same", "id-1", 10f);
            LeaderboardDebuggerService.AddEntry(data, Level, "Same", "id-2", 20f);

            Assert.AreEqual(2, data.GetLevelLeaderBoard(Level).Count);
        }

        [Test]
        public void EdgeTimes_SortCorrectly()
        {
            var entries = LeaderboardGenerator.EdgeTimes.Select((time, index) => new Score { name = $"Edge{index}", id = $"edge-{index}", timescore = time }).ToList();
            var data = LeaderboardDebuggerService.BuildData(Level, entries);

            var problem = LeaderboardDebuggerService.CompareOrder(data.GetLevelLeaderBoard(Level), entries);

            Assert.IsNull(problem, problem);
        }

        [Test]
        public void EdgeTimes_SurviveSaveAndReload()
        {
            var entries = LeaderboardGenerator.EdgeTimes.Select((time, index) => new Score { name = $"Edge{index}", id = $"edge-{index}", timescore = time }).ToList();

            AssertSameAfterReload(LeaderboardDebuggerService.BuildData(Level, entries));
        }

        [Test]
        public void EdgeNames_SurviveSaveAndReload()
        {
            var entries = LeaderboardGenerator.EdgeNames.Append(new string('A', 10000)).Select((text, index) => new Score { name = text, id = text, timescore = index }).ToList();

            AssertSameAfterReload(LeaderboardDebuggerService.BuildData(Level, entries));
        }

        [Test]
        public void EmptyLeaderboard_SurvivesSaveAndReload()
        {
            AssertSameAfterReload(new LeaderBoardData());
        }

        [Test]
        public void SingleEntry_SortsAndSurvivesSaveAndReload()
        {
            var data = LeaderboardDebuggerService.BuildData(Level, Entries(42.5f));

            Assert.AreEqual(1, data.GetLevelLeaderBoard(Level).Count);
            AssertSameAfterReload(data);
        }

        [Test]
        public void MissingLevel_ThrowsKeyNotFound()
        {
            var data = new LeaderBoardData();

            Assert.Throws<KeyNotFoundException>(() => data.GetLevelLeaderBoard(999));
        }

        [Test]
        public void SaveAndLoad_RoundTripKeepsEverything()
        {
            var data = new LeaderBoardData();
            var random = new Random(4);

            for (var level = 1; level <= 3; level++)
            {
                foreach (var entry in LeaderboardGenerator.Generate(50, ScoreDistribution.Normal, 20f, 200f, random))
                {
                    LeaderboardDebuggerService.AddEntry(data, level, entry.name, entry.id, entry.timescore);
                }
            }

            AssertSameAfterReload(data);
        }

        [Test]
        public void MissingFile_CreatesEmptyDefault()
        {
            LeaderboardDebuggerService.DeleteSave();

            var data = LeaderboardDebuggerService.Load();

            Assert.IsNotNull(data);
            Assert.AreEqual(0, data.Score.leaderBoardList.Count);
            Assert.IsTrue(LeaderboardDebuggerService.SaveExists);
        }

        [Test]
        public void CorruptedFile_ResetsToEmptyDefault()
        {
            LeaderboardDebuggerService.WriteGarbage();

            var data = LeaderboardDebuggerService.Load();

            Assert.IsNotNull(data);
            Assert.AreEqual(0, data.Score.leaderBoardList.Count);
        }

        [Test]
        public void TruncatedFile_ResetsToEmptyDefault()
        {
            LeaderboardDebuggerService.Save(LeaderboardDebuggerService.BuildData(Level, Entries(10f, 20f, 30f)));
            LeaderboardDebuggerService.Truncate();

            var data = LeaderboardDebuggerService.Load();

            Assert.IsNotNull(data);
            Assert.AreEqual(0, data.Score.leaderBoardList.Count);
        }

        [Test]
        public void ManuallyEditedFile_LoadsWithoutThrowing()
        {
            LeaderboardDebuggerService.Save(LeaderboardDebuggerService.BuildData(Level, Entries(10f, 20f, 30f)));
            LeaderboardDebuggerService.FlipBytes();

            LeaderBoardData data = null;

            Assert.DoesNotThrow(() => data = LeaderboardDebuggerService.Load());
            Assert.IsNotNull(data);
        }
    }
}
