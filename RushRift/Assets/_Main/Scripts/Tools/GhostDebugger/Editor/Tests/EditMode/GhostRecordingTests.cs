using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.Ghost
{
    /// <summary>
    /// What the recorder keeps during pause, checkpoint reset, finish, countdown and long runs.
    /// </summary>
    public class GhostRecordingTests
    {
        private GhostTestAdapter _adapter;

        [SetUp]
        public void SetUp()
        {
            _adapter = new GhostTestAdapter();
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            _adapter.Dispose();
        }

        [Test]
        public void Pause_RecordsNothing_AndGapIsOneInterval()
        {
            GhostTestClock.StartRecording(_adapter, Vector3.zero);
            GhostTestClock.Feed(_adapter, 0, 15, t => new Vector3(t, 0f, 0f));
            _adapter.SetTime(0.31f);
            _adapter.Feed(new Vector3(0.31f, 0f, 0f), Quaternion.identity);
            _adapter.Pause();

            for (var k = 1; k <= 250; k++)
            {
                _adapter.SetTime(0.31f + k * GhostTestClock.Step);
                _adapter.Feed(new Vector3(0.31f, 0f, 0f), Quaternion.identity);
            }

            Assert.AreEqual(4, _adapter.RecordedRun.Samples.Count, "samples were recorded while paused");

            _adapter.Resume();
            for (var k = 1; k <= 10; k++)
            {
                var runTime = 0.31f + k * GhostTestClock.Step;
                _adapter.SetTime(5.31f + k * GhostTestClock.Step);
                _adapter.Feed(new Vector3(runTime, 0f, 0f), Quaternion.identity);
            }

            var samples = _adapter.RecordedRun.Samples;
            Assert.Greater(samples.Count, 4, "nothing was recorded after resume");
            GhostTestAssert.Near(GhostTestClock.Interval, samples[4].Time - samples[3].Time, GhostTestAssert.TimeTolerance, "gap across the pause");
        }

        [Test]
        public void CheckpointReset_RemovesSamplesAfterCheckpoint()
        {
            GhostTestClock.StartRecording(_adapter, Vector3.zero);
            GhostTestClock.Feed(_adapter, 0, 50, t => new Vector3(t, 0f, 0f));
            _adapter.ReachCheckpoint();
            GhostTestClock.Feed(_adapter, 51, 100, t => new Vector3(t, 0f, 0f));

            _adapter.ResetToCheckpoint();

            var samples = _adapter.RecordedRun.Samples;
            Assert.AreEqual(11, samples.Count, "sample count after reset");
            GhostTestAssert.Near(1f, samples[samples.Count - 1].Time, GhostTestAssert.TimeTolerance, "last sample time");
            foreach (var sample in samples)
            {
                Assert.LessOrEqual(sample.Time, 1f + GhostTestAssert.TimeTolerance, "a sample after the checkpoint was kept");
            }
        }

        [Test]
        public void Finish_LastSampleIsAtCrossing_AndNothingAfterIsKept()
        {
            GhostTestClock.StartRecording(_adapter, Vector3.zero);
            GhostTestClock.Feed(_adapter, 0, 102, t => new Vector3(t * 10f, 0f, 0f));
            _adapter.SetTime(2.05f);
            _adapter.Finish(new Vector3(20.5f, 0f, 0f), Quaternion.identity);
            var countAtFinish = _adapter.RecordedRun.Samples.Count;

            GhostTestClock.Feed(_adapter, 103, 150, t => new Vector3(t * 10f, 0f, 0f));

            var run = _adapter.RecordedRun;
            var last = run.Samples[run.Samples.Count - 1];
            Assert.AreEqual(countAtFinish, run.Samples.Count, "samples were recorded after the finish");
            GhostTestAssert.Near(2.05f, last.Time, GhostTestAssert.TimeTolerance, "last sample time");
            GhostTestAssert.Near(new Vector3(20.5f, 0f, 0f), last.Position, GhostTestAssert.PositionTolerance, "last sample position");
            GhostTestAssert.Near(2.05f, run.RunTime, GhostTestAssert.TimeTolerance, "run time");
        }

        [Test]
        public void Countdown_RecordsNothingBeforeControl()
        {
            _adapter.SetRecordInterval(GhostTestClock.Interval);
            _adapter.SetTime(0f);
            _adapter.BeginRecording();
            GhostTestClock.Feed(_adapter, 0, 150, t => new Vector3(t, 0f, 0f));

            Assert.AreEqual(0, _adapter.RecordedRun.Samples.Count, "samples were recorded during the countdown");

            _adapter.SetTime(3f);
            _adapter.GiveControl(new Vector3(5f, 0f, 0f), Quaternion.identity);
            GhostTestClock.Feed(_adapter, 150, 175, t => new Vector3(5f + t - 3f, 0f, 0f));

            var samples = _adapter.RecordedRun.Samples;
            Assert.AreEqual(6, samples.Count, "sample count after control");
            GhostTestAssert.Near(0f, samples[0].Time, GhostTestAssert.TimeTolerance, "first sample time");
            GhostTestAssert.Near(new Vector3(5f, 0f, 0f), samples[0].Position, GhostTestAssert.PositionTolerance, "first sample position");
        }

        [Test]
        public void LongRun_CapsAtSampleLimitWithoutErrors()
        {
            var limit = _adapter.SampleLimit;
            Assert.Greater(limit, 0, "sample limit");
            LogAssert.ignoreFailingMessages = true;

            using (var log = new GhostTestLogCapture())
            {
                Assert.DoesNotThrow(() =>
                {
                    GhostTestClock.StartRecording(_adapter, Vector3.zero);
                    GhostTestClock.Feed(_adapter, 0, (limit + 50) * 5, t => new Vector3(t, 0f, 0f));
                });

                Assert.LessOrEqual(_adapter.RecordedRun.Samples.Count, limit, "sample count above the limit");
                Assert.AreEqual(0, log.Errors, "an error was logged");
                Assert.AreEqual(0, log.Exceptions, "an exception was logged");
            }
        }
    }
}
