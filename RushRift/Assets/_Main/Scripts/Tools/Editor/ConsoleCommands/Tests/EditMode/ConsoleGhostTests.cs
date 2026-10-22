using _Main.Scripts.Ghost;
using Game.Tools.DebugCommands;
using NUnit.Framework;
using UnityEngine;

namespace Game.ConsoleCommands.Tests
{
    /// <summary>
    /// disable-ghost and enable-ghost against fake services, and the GhostPlayer toggle they use.
    /// </summary>
    public class ConsoleGhostTests
    {
        private const string Password = "pw";

        private FakeConsoleServices services;
        private ConsoleCommandSet commands;
        private GameObject ghostObject;

        [SetUp]
        public void SetUp()
        {
            services = new FakeConsoleServices();
            commands = new ConsoleCommandSet(new ConsoleSession(Password), services, null);
            Assert.IsTrue(commands.TryExecute($"login {Password}", out _));
        }

        [TearDown]
        public void TearDown()
        {
            GhostPlayer.SetGhostEnabled(true);
            if (ghostObject) Object.DestroyImmediate(ghostObject);
        }

        private ConsoleResult Run(string input)
        {
            Assert.IsTrue(commands.TryExecute(input, out var result));
            return result;
        }

        [Test]
        public void DisableGhost_TurnsGhostOff()
        {
            var result = Run("disable-ghost");

            Assert.IsTrue(result.Success, result.Message);
            Assert.IsFalse(services.GhostEnabled);
        }

        [Test]
        public void EnableGhost_TurnsGhostBackOn()
        {
            Run("disable-ghost");

            var result = Run("enable-ghost");

            Assert.IsTrue(result.Success, result.Message);
            Assert.IsTrue(services.GhostEnabled);
        }

        [Test]
        public void DisableGhost_Twice_IsSafe()
        {
            Assert.IsTrue(Run("disable-ghost").Success);
            var second = Run("disable-ghost");

            Assert.IsTrue(second.Success, second.Message);
            StringAssert.Contains("already", second.Message);
            Assert.IsFalse(services.GhostEnabled);
        }

        [Test]
        public void EnableGhost_Twice_IsSafe()
        {
            Assert.IsTrue(Run("enable-ghost").Success);
            var second = Run("enable-ghost");

            Assert.IsTrue(second.Success, second.Message);
            Assert.IsTrue(services.GhostEnabled);
        }

        [TestCase("disable-ghost")]
        [TestCase("enable-ghost")]
        public void GhostCommands_OutsidePlayMode_Fail(string input)
        {
            services.IsPlaying = false;

            var result = Run(input);

            Assert.IsFalse(result.Success);
            StringAssert.Contains("Play Mode", result.Message);
            Assert.AreEqual(0, services.GhostCalls);
        }

        [Test]
        public void GhostPlayerToggle_DisablesAndEnablesExistingGhosts()
        {
            ghostObject = new GameObject("ConsoleGhostTest");
            var player = ghostObject.AddComponent<GhostPlayer>();

            GhostPlayer.SetGhostEnabled(false);

            Assert.IsFalse(GhostPlayer.GhostEnabled);
            Assert.IsFalse(player.enabled);

            GhostPlayer.SetGhostEnabled(false);
            Assert.IsFalse(player.enabled);

            GhostPlayer.SetGhostEnabled(true);

            Assert.IsTrue(GhostPlayer.GhostEnabled);
            Assert.IsTrue(player.enabled);

            GhostPlayer.SetGhostEnabled(true);
            Assert.IsTrue(player.enabled);
        }

        [Test]
        public void GhostPlayerToggle_LeavesGhostsDisabledByDesignAlone()
        {
            ghostObject = new GameObject("ConsoleGhostTest");
            var player = ghostObject.AddComponent<GhostPlayer>();
            player.enabled = false;

            GhostPlayer.SetGhostEnabled(false);
            GhostPlayer.SetGhostEnabled(true);

            Assert.IsFalse(player.enabled);
        }
    }
}
