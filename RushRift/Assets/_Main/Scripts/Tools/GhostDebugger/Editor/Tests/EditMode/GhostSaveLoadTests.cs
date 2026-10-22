using System;
using System.IO;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Game.Tests.Ghost
{
    /// <summary>
    /// Saving, loading and rejecting bad ghost files in a temporary folder.
    /// </summary>
    public class GhostSaveLoadTests
    {
        public enum BadFile
        {
            Corrupted,
            Empty,
            Truncated,
            Outdated
        }

        private GhostTestAdapter _adapter;
        private GhostTestTempFolder _folder;

        [SetUp]
        public void SetUp()
        {
            _adapter = new GhostTestAdapter();
            _folder = new GhostTestTempFolder();
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            _adapter.Dispose();
            _folder.Dispose();
        }

        [Test]
        public void SaveThenLoad_ReturnsSameRun()
        {
            var run = GhostTestRuns.RandomRun(42, 200, 0.1f);
            run.Version = _adapter.CurrentVersion;
            var path = _folder.File("run.ghost");

            _adapter.Save(run, path);
            var result = _adapter.Load(path);

            Assert.IsTrue(result.IsValid, "loaded run is not valid");
            Assert.AreEqual(run.Samples.Count, result.Run.Samples.Count, "sample count");
            Assert.AreEqual(run.Version, result.Run.Version, "version");
            GhostTestAssert.Near(19.9f, result.Run.RunTime, GhostTestAssert.TimeTolerance, "run time");
            for (var i = 0; i < run.Samples.Count; i++)
            {
                var expected = run.Samples[i];
                var actual = result.Run.Samples[i];
                GhostTestAssert.Near(expected.Time, actual.Time, GhostTestAssert.TimeTolerance, $"time of sample {i}");
                GhostTestAssert.Near(expected.Position, actual.Position, GhostTestAssert.PositionTolerance, $"position of sample {i}");
                GhostTestAssert.Near(expected.Rotation, actual.Rotation, GhostTestAssert.RotationTolerance, $"rotation of sample {i}");
            }
        }

        [TestCase(BadFile.Corrupted)]
        [TestCase(BadFile.Empty)]
        [TestCase(BadFile.Truncated)]
        [TestCase(BadFile.Outdated)]
        public void Load_BadFile_ReturnsInvalidAndLogsError(BadFile kind)
        {
            var path = WriteBadFile(kind);
            GhostTestLoadResult result = null;
            LogAssert.ignoreFailingMessages = true;

            using (var log = new GhostTestLogCapture())
            {
                Assert.DoesNotThrow(() => result = _adapter.Load(path));
                Assert.IsNotNull(result, "load returned null");
                Assert.IsFalse(result.IsValid, "bad file loaded as valid");
                Assert.GreaterOrEqual(log.Errors, 1, "no LogType.Error was logged");
                Assert.AreEqual(0, log.Exceptions, "an exception was logged");
            }
        }

        private string WriteBadFile(BadFile kind)
        {
            var path = _folder.File($"{kind}.ghost");
            if (kind == BadFile.Outdated)
            {
                _adapter.WriteOutdatedVersionFile(path);
                return path;
            }

            if (kind == BadFile.Empty)
            {
                File.WriteAllBytes(path, Array.Empty<byte>());
                return path;
            }

            var validPath = _folder.File("valid.ghost");
            var run = GhostTestRuns.RandomRun(42, 200, 0.1f);
            run.Version = _adapter.CurrentVersion;
            _adapter.Save(run, validPath);
            var valid = File.ReadAllBytes(validPath);

            if (kind == BadFile.Truncated)
            {
                var half = new byte[valid.Length / 2];
                Array.Copy(valid, half, half.Length);
                File.WriteAllBytes(path, half);
                return path;
            }

            var corrupted = new byte[valid.Length];
            new Random(7).NextBytes(corrupted);
            File.WriteAllBytes(path, corrupted);
            return path;
        }
    }
}
