using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.GhostDebugger.Tests
{
    /// <summary>
    /// Rules for replacing the saved best ghost.
    /// </summary>
    public class GhostBestRunTests
    {
        public enum InvalidRun
        {
            NoSamples,
            Unfinished,
            NaNPosition
        }

        private static readonly Vector3 OldMarker = new(1f, 1f, 1f);
        private static readonly Vector3 NewMarker = new(2f, 2f, 2f);

        private GhostTestAdapter _adapter;
        private GhostTestTempFolder _folder;

        [SetUp]
        public void SetUp()
        {
            _adapter = new GhostTestAdapter();
            _folder = new GhostTestTempFolder();
            _adapter.SetBestRunFolder(_folder.Path);
            Assert.IsTrue(_adapter.SubmitRun(Stamp(GhostTestRuns.MarkedRun(30f, OldMarker))), "the first 30.0 s run was not saved");
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            _adapter.Dispose();
            _folder.Dispose();
        }

        [Test]
        [Description("With a 30.0 s ghost saved, finishes a 29.5 s run and checks the new run replaces it.")]
        public void FasterRun_ReplacesSavedGhost()
        {
            var replaced = _adapter.SubmitRun(Stamp(GhostTestRuns.MarkedRun(29.5f, NewMarker)));

            Assert.IsTrue(replaced, "faster run was not saved");
            AssertSaved(29.5f, NewMarker);
        }

        [Test]
        [Description("With a 30.0 s ghost saved, finishes a 31.0 s run and checks the old ghost is kept.")]
        public void SlowerRun_KeepsSavedGhost()
        {
            var replaced = _adapter.SubmitRun(Stamp(GhostTestRuns.MarkedRun(31f, NewMarker)));

            Assert.IsFalse(replaced, "slower run was saved");
            AssertSaved(30f, OldMarker);
        }

        [Test]
        [Description("With a 30.0 s ghost saved, finishes another 30.0 s run and checks the old ghost is kept.")]
        public void TiedRun_KeepsSavedGhost()
        {
            var replaced = _adapter.SubmitRun(Stamp(GhostTestRuns.MarkedRun(30f, NewMarker)));

            Assert.IsFalse(replaced, "tied run was saved");
            AssertSaved(30f, OldMarker);
        }

        [TestCase(InvalidRun.NoSamples)]
        [TestCase(InvalidRun.Unfinished)]
        [TestCase(InvalidRun.NaNPosition)]
        [Description("Submits a fast but invalid run (no samples, not finished, or a NaN position) and checks it is never saved over the real best.")]
        public void InvalidRun_IsNeverSaved(InvalidRun kind)
        {
            LogAssert.ignoreFailingMessages = true;
            var run = Stamp(GhostTestRuns.MarkedRun(10f, NewMarker));
            if (kind == InvalidRun.NoSamples)
            {
                run.Samples.Clear();
            }
            else if (kind == InvalidRun.Unfinished)
            {
                run.Finished = false;
            }
            else
            {
                var sample = run.Samples[3];
                sample.Position = new Vector3(float.NaN, 0f, 0f);
                run.Samples[3] = sample;
            }

            var replaced = _adapter.SubmitRun(run);

            Assert.IsFalse(replaced, $"invalid run ({kind}) was saved");
            AssertSaved(30f, OldMarker);
        }

        private GhostTestRun Stamp(GhostTestRun run)
        {
            run.Version = _adapter.CurrentVersion;
            return run;
        }

        private void AssertSaved(float runTime, Vector3 marker)
        {
            var saved = _adapter.LoadBestRun();
            Assert.IsTrue(saved.IsValid, "saved best run is not valid");
            GhostTestAssert.Near(runTime, saved.Run.RunTime, GhostTestAssert.TimeTolerance, "saved run time");
            GhostTestAssert.Near(marker, saved.Run.Samples[0].Position, GhostTestAssert.PositionTolerance, "saved run marker");
        }
    }
}
