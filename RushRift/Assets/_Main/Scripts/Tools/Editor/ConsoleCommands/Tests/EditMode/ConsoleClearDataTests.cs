using System.IO;
using Game.Saves;
using Game.Tools.DebugCommands;
using NUnit.Framework;

namespace Game.ConsoleCommands.Tests
{
    /// <summary>
    /// The clear commands against the real SaveSystem. The real files are backed up first and restored afterwards.
    /// </summary>
    public class ConsoleClearDataTests
    {
        private const string Password = "pw";
        private const int TestLevel = 999;

        private ConsoleTestDataBackup backup;
        private ConsoleCommandSet commands;

        [SetUp]
        public void SetUp()
        {
            backup = new ConsoleTestDataBackup();
            commands = new ConsoleCommandSet(new ConsoleSession(Password), new GameConsoleServices(), null);
            Seed();
        }

        [TearDown]
        public void TearDown()
        {
            backup.Dispose();
        }

        private void Seed()
        {
            var save = SaveSystem.LoadGame();
            save.SetNewBestTime(TestLevel, 12.5f);
            save.SaveGame();

            var settings = SaveSystem.LoadSettings();
            settings.Camera.invertX = true;
            SaveSystem.SaveSettings(settings);

            var leaderboard = SaveSystem.LoadLeaderBoard();
            leaderboard.AddTimeToLevel(TestLevel, new PlayerID { name = "ConsoleTest", id = "console-test" }, 10f);
            SaveSystem.SaveLeaderBoard(leaderboard);

            File.WriteAllText(Path.Combine(backup.GhostFolder, "level_ConsoleTest.ghost.json"), "{}");

            Assert.IsTrue(HasSave, "the save was not seeded");
            Assert.IsTrue(HasSettings, "the settings were not seeded");
            Assert.IsTrue(HasLeaderboard, "the leaderboard was not seeded");
        }

        private static bool HasSave => SaveSystem.LoadGame().BestTimes.ContainsKey(TestLevel);
        private static bool HasSettings => SaveSystem.LoadSettings().Camera.invertX;
        private static bool HasLeaderboard => SaveSystem.LoadLeaderBoard().Score.leaderBoardList.ContainsKey(TestLevel);
        private bool HasGhosts => Directory.Exists(backup.GhostFolder);

        private ConsoleResult Run(string input)
        {
            Assert.IsTrue(commands.TryExecute($"login {Password}", out _));
            Assert.IsTrue(commands.TryExecute(input, out var result));
            return result;
        }

        [Test]
        public void ClearSettings_ResetsOnlySettings()
        {
            var result = Run("clear-settings-data");

            Assert.IsTrue(result.Success, result.Message);
            Assert.IsFalse(HasSettings);
            Assert.IsTrue(HasSave);
            Assert.IsTrue(HasLeaderboard);
            Assert.IsTrue(HasGhosts);
        }

        [Test]
        public void ClearSave_ResetsOnlySaveAndGhosts()
        {
            var result = Run("clear-save-data");

            Assert.IsTrue(result.Success, result.Message);
            Assert.IsFalse(HasSave);
            Assert.IsFalse(HasGhosts);
            Assert.IsTrue(HasSettings);
            Assert.IsTrue(HasLeaderboard);
        }

        [Test]
        public void ClearLeaderboard_ResetsOnlyLeaderboard()
        {
            var result = Run("clear-leaderboard-data");

            Assert.IsTrue(result.Success, result.Message);
            Assert.IsFalse(HasLeaderboard);
            Assert.IsTrue(HasSave);
            Assert.IsTrue(HasSettings);
            Assert.IsTrue(HasGhosts);
        }

        [TestCase("clear-settings-data")]
        [TestCase("clear-save-data")]
        [TestCase("clear-leaderboard-data")]
        public void ClearCommand_LoggedOut_KeepsAllData(string input)
        {
            Assert.IsTrue(commands.TryExecute(input, out var result));

            Assert.IsFalse(result.Success);
            Assert.IsTrue(HasSave);
            Assert.IsTrue(HasSettings);
            Assert.IsTrue(HasLeaderboard);
            Assert.IsTrue(HasGhosts);
        }
    }
}
