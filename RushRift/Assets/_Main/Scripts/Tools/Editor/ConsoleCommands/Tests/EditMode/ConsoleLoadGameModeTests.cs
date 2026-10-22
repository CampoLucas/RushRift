using System.Collections.Generic;
using System.Reflection;
using Game.Levels;
using Game.Tools.DebugCommands;
using NUnit.Framework;
using UnityEngine;

namespace Game.ConsoleCommands.Tests
{
    /// <summary>
    /// load-gm against fake services and game modes made in code.
    /// </summary>
    public class ConsoleLoadGameModeTests
    {
        private const string Password = "pw";

        private static readonly FieldInfo LevelsField = typeof(GameModeSO).GetField("levels", BindingFlags.NonPublic | BindingFlags.Instance);

        private readonly List<Object> created = new List<Object>();
        private FakeConsoleServices services;
        private GameModeSO sectors;
        private GameModeSO rushes;
        private BaseLevelSO firstLevel;

        [SetUp]
        public void SetUp()
        {
            services = new FakeConsoleServices();
            firstLevel = CreateLevel("SectorOne");
            sectors = CreateMode("SectorsGameMode", firstLevel, CreateLevel("SectorTwo"));
            rushes = CreateMode("RushesGameMode", CreateLevel("RushOne"));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var item in created) Object.DestroyImmediate(item);
            created.Clear();
        }

        private BaseLevelSO CreateLevel(string name)
        {
            var level = ScriptableObject.CreateInstance<LevelSO>();
            level.name = name;
            created.Add(level);
            return level;
        }

        private GameModeSO CreateMode(string name, params BaseLevelSO[] levels)
        {
            var mode = ScriptableObject.CreateInstance<GameModeSO>();
            mode.name = name;
            LevelsField.SetValue(mode, new List<BaseLevelSO>(levels));
            created.Add(mode);
            return mode;
        }

        private ConsoleResult Run(string input, params GameModeSO[] modes)
        {
            var commands = new ConsoleCommandSet(new ConsoleSession(Password), services, modes);
            Assert.IsTrue(commands.TryExecute($"login {Password}", out _));
            Assert.IsTrue(commands.TryExecute(input, out var result));
            return result;
        }

        [Test]
        public void ValidId_LoadsTheModeOnItsFirstLevel()
        {
            var result = Run("load-gm SectorsGameMode", sectors, rushes);

            Assert.IsTrue(result.Success, result.Message);
            Assert.AreEqual(1, services.LoadCalls);
            Assert.AreSame(sectors, services.LoadedMode);
            Assert.AreSame(firstLevel, services.LoadedLevel);
        }

        [Test]
        public void Id_IsCaseInsensitive()
        {
            var result = Run("load-gm rushesgamemode", sectors, rushes);

            Assert.IsTrue(result.Success, result.Message);
            Assert.AreSame(rushes, services.LoadedMode);
        }

        [Test]
        public void InvalidId_ListsValidIdsAndLoadsNothing()
        {
            var result = Run("load-gm Nope", sectors, rushes);

            Assert.IsFalse(result.Success);
            StringAssert.Contains("Nope", result.Message);
            StringAssert.Contains("SectorsGameMode", result.Message);
            StringAssert.Contains("RushesGameMode", result.Message);
            Assert.AreEqual(0, services.LoadCalls);
        }

        [Test]
        public void MissingArgument_GivesUsageErrorAndLoadsNothing()
        {
            var result = Run("load-gm", sectors);

            Assert.IsFalse(result.Success);
            StringAssert.Contains("Usage", result.Message);
            Assert.AreEqual(0, services.LoadCalls);
        }

        [Test]
        public void TooManyArguments_GivesUsageErrorAndLoadsNothing()
        {
            var result = Run("load-gm SectorsGameMode 2", sectors);

            Assert.IsFalse(result.Success);
            StringAssert.Contains("Usage", result.Message);
            Assert.AreEqual(0, services.LoadCalls);
        }

        [Test]
        public void ModeWithoutLevels_FailsAndLoadsNothing()
        {
            var empty = CreateMode("EmptyGameMode");

            var result = Run("load-gm EmptyGameMode", empty);

            Assert.IsFalse(result.Success);
            StringAssert.Contains("no first level", result.Message);
            Assert.AreEqual(0, services.LoadCalls);
        }

        [Test]
        public void NoModesConfigured_Fails()
        {
            var result = Run("load-gm SectorsGameMode");

            Assert.IsFalse(result.Success);
            StringAssert.Contains("No game modes", result.Message);
            Assert.AreEqual(0, services.LoadCalls);
        }

        [Test]
        public void NotInPlayMode_FailsAndLoadsNothing()
        {
            services.IsPlaying = false;

            var result = Run("load-gm SectorsGameMode", sectors);

            Assert.IsFalse(result.Success);
            StringAssert.Contains("Play Mode", result.Message);
            Assert.AreEqual(0, services.LoadCalls);
        }

        [Test]
        public void NullModes_AreSkipped()
        {
            var result = Run("load-gm SectorsGameMode", null, sectors, null);

            Assert.IsTrue(result.Success, result.Message);
            Assert.AreSame(sectors, services.LoadedMode);
        }

        [Test]
        public void GameModeIds_AreTheAssetNames()
        {
            var commands = new ConsoleCommandSet(new ConsoleSession(Password), services, new[] { sectors, null, rushes });

            CollectionAssert.AreEqual(new[] { "SectorsGameMode", "RushesGameMode" }, commands.GameModeIds);
        }
    }
}
