#if LEADERBOARD_CMD_ENABLED
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Game.Levels;
using Game.Tools.DebugCommands;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.LeaderboardDebugger.Tests
{
    public class LeaderboardCommandTests
    {
        private const int LevelId = 7;

        private static readonly PropertyInfo SessionProperty = typeof(GlobalLevelManager).GetProperty("CurrentSession", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly FieldInfo LevelIdField = typeof(BaseLevelSO).GetField("levelID", BindingFlags.NonPublic | BindingFlags.Instance);

        private readonly List<Object> created = new List<Object>();
        private SaveBackup backup;
        private object previousSession;

        [SetUp]
        public void SetUp()
        {
            backup = LeaderboardDebuggerService.Capture();
            previousSession = SessionProperty.GetValue(null);
            SessionProperty.SetValue(null, default(NullCheck<GameSessionSO>));
            LeaderboardDebuggerService.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            SessionProperty.SetValue(null, previousSession);
            foreach (var item in created) Object.DestroyImmediate(item);
            created.Clear();
            backup.Restore();
        }

        private void UseLevel(int id)
        {
            var level = ScriptableObject.CreateInstance<LevelSO>();
            LevelIdField.SetValue(level, id);
            var session = ScriptableObject.CreateInstance<GameSessionSO>();
            session.Initialize(null, level);
            created.Add(level);
            created.Add(session);
            SessionProperty.SetValue(null, new NullCheck<GameSessionSO>(session));
        }

        private static List<Score> Entries(int level)
        {
            return LeaderboardDebuggerService.GetEntries(LeaderboardDebuggerService.Load(), level);
        }

        private static void AddExisting(int level, string name, float time)
        {
            LeaderboardDebuggerService.AddEntries(level, new[] { new Score { name = name, id = name, timescore = time } });
        }

        private static void ExpectWarning(string pattern)
        {
            LogAssert.Expect(LogType.Warning, new Regex(pattern));
        }

        [Test]
        public void Add_NameOnly_UsesCurrentLevelAndCurrentTime()
        {
            UseLevel(LevelId);

            var added = new LbAddCmd().Do(new[] { "Alice" });

            Assert.IsTrue(added);
            var entries = Entries(LevelId);
            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual("Alice", entries[0].name);
            Assert.AreEqual(GlobalLevelManager.CurrentTime, entries[0].timescore);
        }

        [Test]
        public void Add_WithTimeAndLevel_UsesThem()
        {
            var added = new LbAddCmd().Do(new[] { "Bob", "12.5", "3" });

            Assert.IsTrue(added);
            var entries = Entries(3);
            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual(12.5f, entries[0].timescore);
        }

        [Test]
        public void Add_WithTimeOnly_UsesCurrentLevel()
        {
            UseLevel(LevelId);

            var added = new LbAddCmd().Do(new[] { "Cara", "20" });

            Assert.IsTrue(added);
            Assert.AreEqual(20f, Entries(LevelId)[0].timescore);
        }

        [Test]
        public void Add_SameNameTwice_OverwritesEntry()
        {
            var command = new LbAddCmd();
            command.Do(new[] { "Dan", "30", "3" });
            command.Do(new[] { "Dan", "10", "3" });

            var entries = Entries(3);

            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual(10f, entries[0].timescore);
        }

        [Test]
        public void Add_NoArgs_FailsAndSavesNothing()
        {
            ExpectWarning("It cannot be added");

            var added = new LbAddCmd().Do(new string[0]);

            Assert.IsFalse(added);
            Assert.AreEqual(0, LeaderboardDebuggerService.Load().Score.leaderBoardList.Count);
        }

        [Test]
        public void Add_BlankName_FailsAndSavesNothing()
        {
            ExpectWarning("It cannot be added");

            var added = new LbAddCmd().Do(new[] { " ", "10", "3" });

            Assert.IsFalse(added);
            Assert.AreEqual(0, LeaderboardDebuggerService.Load().Score.leaderBoardList.Count);
        }

        [Test]
        public void Add_InvalidTime_FailsAndSavesNothing()
        {
            ExpectWarning("It cannot be added");

            var added = new LbAddCmd().Do(new[] { "Eve", "fast", "3" });

            Assert.IsFalse(added);
            Assert.AreEqual(0, LeaderboardDebuggerService.Load().Score.leaderBoardList.Count);
        }

        [Test]
        public void Add_InvalidLevelId_FailsAndSavesNothing()
        {
            ExpectWarning("It cannot be added");

            var added = new LbAddCmd().Do(new[] { "Fay", "10", "abc" });

            Assert.IsFalse(added);
            Assert.AreEqual(0, LeaderboardDebuggerService.Load().Score.leaderBoardList.Count);
        }

        [Test]
        public void Add_NoLevelArgAndNoActiveLevel_FailsAndSavesNothing()
        {
            ExpectWarning("It cannot be added");

            var added = new LbAddCmd().Do(new[] { "Gus", "10" });

            Assert.IsFalse(added);
            Assert.AreEqual(0, LeaderboardDebuggerService.Load().Score.leaderBoardList.Count);
        }

        [Test]
        public void Fill_AddsExactlyTheRequestedCount()
        {
            UseLevel(LevelId);

            var filled = new LbFillCmd().Do(25);

            Assert.IsTrue(filled);
            Assert.AreEqual(25, Entries(LevelId).Count);
        }

        [Test]
        public void Fill_ZeroOrNegativeCount_Fails()
        {
            UseLevel(LevelId);
            ExpectWarning("greater than 0");
            ExpectWarning("greater than 0");

            Assert.IsFalse(new LbFillCmd().Do(0));
            Assert.IsFalse(new LbFillCmd().Do(-3));
            Assert.AreEqual(0, Entries(LevelId).Count);
        }

        [Test]
        public void Fill_NoActiveLevel_Fails()
        {
            ExpectWarning("no active level");

            Assert.IsFalse(new LbFillCmd().Do(5));
            Assert.AreEqual(0, LeaderboardDebuggerService.Load().Score.leaderBoardList.Count);
        }

        [Test]
        public void Ties_AddEntriesThatShareOneTime()
        {
            UseLevel(LevelId);

            var added = new LbTiesCmd().Do(10);

            Assert.IsTrue(added);
            var entries = Entries(LevelId);
            Assert.AreEqual(10, entries.Count);
            Assert.AreEqual(1, entries.Select(entry => entry.timescore).Distinct().Count());
        }

        [Test]
        public void Ties_NoActiveLevel_Fails()
        {
            ExpectWarning("no active level");

            Assert.IsFalse(new LbTiesCmd().Do(5));
        }

        [Test]
        public void ClearLevel_RemovesOnlyTheCurrentLevel()
        {
            UseLevel(LevelId);
            AddExisting(LevelId, "Hal", 10f);
            AddExisting(3, "Ivy", 20f);

            var cleared = new LbClearLevelCmd().Do();

            Assert.IsTrue(cleared);
            Assert.AreEqual(0, Entries(LevelId).Count);
            Assert.AreEqual(1, Entries(3).Count);
        }

        [Test]
        public void ClearLevel_NoActiveLevel_FailsAndKeepsData()
        {
            AddExisting(3, "Jay", 20f);
            ExpectWarning("no active level");

            Assert.IsFalse(new LbClearLevelCmd().Do());
            Assert.AreEqual(1, Entries(3).Count);
        }

        [Test]
        public void ClearAll_EmptiesEveryLevel()
        {
            AddExisting(1, "Kim", 10f);
            AddExisting(2, "Lee", 20f);

            var cleared = new LbClearAllCmd().Do();

            Assert.IsTrue(cleared);
            Assert.AreEqual(0, LeaderboardDebuggerService.Load().Score.leaderBoardList.Count);
        }

        [Test]
        public void List_WithEntries_ReturnsTrueAndLogsThem()
        {
            UseLevel(LevelId);
            AddExisting(LevelId, "Max", 10f);
            LogAssert.Expect(LogType.Log, new Regex("Max"));

            Assert.IsTrue(new LbListCmd().Do());
        }

        [Test]
        public void List_WithoutEntries_ReturnsTrueAndSaysSo()
        {
            UseLevel(LevelId);
            LogAssert.Expect(LogType.Log, new Regex("has no entries"));

            Assert.IsTrue(new LbListCmd().Do());
        }

        [Test]
        public void List_NoActiveLevel_Fails()
        {
            ExpectWarning("no active level");

            Assert.IsFalse(new LbListCmd().Do());
        }

        [Test]
        public void CommandIds_AreNotSubstringsOfEachOther()
        {
            var ids = LeaderboardCmdUtils.CreateCommands().Cast<DebugCommandBase>().Select(command => command.ID).ToList();

            Assert.AreEqual(ids.Count, ids.Distinct().Count());

            foreach (var id in ids)
            {
                foreach (var other in ids.Where(candidate => candidate != id))
                {
                    Assert.IsFalse(other.Contains(id), $"'{id}' is contained in '{other}', so the console would run '{id}' when '{other}' is typed");
                }
            }
        }
    }
}
#endif
