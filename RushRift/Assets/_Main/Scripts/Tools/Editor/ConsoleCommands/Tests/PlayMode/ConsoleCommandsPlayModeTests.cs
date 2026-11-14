using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.ConsoleCommands.Tests
{
    /// <summary>
    /// Console commands in a running game: loading a real game mode and toggling a real ghost.
    /// </summary>
    public class ConsoleCommandsPlayModeTests
    {
        private const float LoadTimeout = 60f;
        private const int SettleFrames = 30;

        private IConsoleCommandsTestBackend _backend;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _backend = ConsoleCommandsTestBackendLocator.Create();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            _backend.Dispose();
            yield return null;
        }

        private IEnumerator WaitWhileLoading()
        {
            var start = Time.realtimeSinceStartup;

            for (var i = 0; i < SettleFrames; i++) yield return null;

            while (_backend.IsLoading && Time.realtimeSinceStartup - start < LoadTimeout) yield return null;
        }

        [UnityTest]
        [Description("Logs in, runs load-gm with a real game mode and waits until that mode and its first level are the current session.")]
        public IEnumerator LoadGameMode_LoadsFirstLevelOfTheMode()
        {
            var modeId = _backend.LoadableGameModeId;
            if (modeId == null) Assert.Ignore("The project has no game mode with a first level.");
            if (_backend.MissingScenesReason != null) Assert.Ignore(_backend.MissingScenesReason);

            LogAssert.ignoreFailingMessages = true;

            yield return WaitWhileLoading();

            Assert.IsTrue(_backend.Execute($"load-gm {modeId}", out var message), message);

            var expectedLevel = _backend.FirstLevelId(modeId);
            var start = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - start < LoadTimeout)
            {
                if (!_backend.IsLoading && _backend.CurrentGameModeId == modeId && _backend.CurrentLevelId == expectedLevel) break;
                yield return null;
            }

            Assert.AreEqual(modeId, _backend.CurrentGameModeId, "the current game mode did not change");
            Assert.AreEqual(expectedLevel, _backend.CurrentLevelId, "the current level is not the first level of the mode");
            Assert.IsFalse(_backend.IsLoading, "loading did not finish in time");
        }

        [UnityTest]
        [Description("A ghost spawned while the ghost is disabled stays off, enable-ghost turns it on, and running each command twice is safe.")]
        public IEnumerator GhostToggle_AppliesToNewGhostsAndIsSafeTwice()
        {
            Assert.IsTrue(_backend.Execute("disable-ghost", out var message), message);
            Assert.IsTrue(_backend.Execute("disable-ghost", out message), message);
            Assert.IsFalse(_backend.GhostEnabled);

            var ghost = _backend.SpawnGhostPlayer();
            yield return null;

            Assert.IsFalse(_backend.IsGhostPlayerEnabled(ghost), "a ghost spawned while disabled was enabled");

            Assert.IsTrue(_backend.Execute("enable-ghost", out message), message);
            Assert.IsTrue(_backend.Execute("enable-ghost", out message), message);
            yield return null;

            Assert.IsTrue(_backend.GhostEnabled);
            Assert.IsTrue(_backend.IsGhostPlayerEnabled(ghost), "enable-ghost did not turn the ghost back on");
        }
    }
}
