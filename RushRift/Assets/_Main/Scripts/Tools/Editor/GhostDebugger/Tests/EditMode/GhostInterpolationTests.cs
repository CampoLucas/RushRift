using NUnit.Framework;
using UnityEngine;

namespace Game.GhostDebugger.Tests
{
    /// <summary>
    /// Position interpolation of a known run under a fake clock.
    /// </summary>
    public class GhostInterpolationTests
    {
        private GhostTestAdapter _adapter;
        private GhostTestRun _run;

        [SetUp]
        public void SetUp()
        {
            _adapter = new GhostTestAdapter();
            _run = GhostTestRuns.RunA();
            _adapter.LoadForPlayback(_run);
        }

        [TearDown]
        public void TearDown()
        {
            _adapter.Dispose();
        }

        [Test]
        [Description("Plays a known 4-sample run with a fake clock in 0.01 s steps and checks every position is the straight-line blend of the two samples around it (tolerance 0.001).")]
        public void Position_AtEveryStep_MatchesLinearInterpolation()
        {
            for (var k = 0; k <= 30; k++)
            {
                var segment = Mathf.Min(k / 10, 2);
                var fraction = (k - segment * 10) / 10f;
                var expected = Vector3.Lerp(_run.Samples[segment].Position, _run.Samples[segment + 1].Position, fraction);

                _adapter.Evaluate(k * 0.01f, out var position, out _);

                GhostTestAssert.Near(expected, position, GhostTestAssert.PositionTolerance, $"t = {k * 0.01f:F2}");
            }
        }

        [TestCase(0.05f, 0.5f, 1f, 0f)]
        [TestCase(0.15f, 2f, 2f, -0.5f)]
        [TestCase(0.25f, 3f, 3.5f, -1f)]
        [Description("Checks the ghost is exactly halfway between two samples at the midpoint time (tolerance 0.001).")]
        public void Position_AtMidpoint_IsHalfwayBetweenSamples(float time, float x, float y, float z)
        {
            _adapter.Evaluate(time, out var position, out _);

            GhostTestAssert.Near(new Vector3(x, y, z), position, GhostTestAssert.PositionTolerance, $"t = {time:F2}");
        }

        [TestCase(0, 0.0f)]
        [TestCase(1, 0.1f)]
        [TestCase(2, 0.2f)]
        [TestCase(3, 0.3f)]
        [Description("Checks that at a sample's exact time the ghost is exactly on that sample (tolerance 0.001).")]
        public void Position_OnSampleTime_IsSamplePosition(int index, float time)
        {
            _adapter.Evaluate(time, out var position, out _);

            GhostTestAssert.Near(_run.Samples[index].Position, position, GhostTestAssert.PositionTolerance, $"sample {index}");
        }

        [Test]
        [Description("Asks for a time before the run starts and checks the ghost stays on the first sample.")]
        public void Position_BeforeStart_IsClampedToFirstSample()
        {
            _adapter.Evaluate(-1f, out var position, out _);

            GhostTestAssert.Near(_run.Samples[0].Position, position, GhostTestAssert.PositionTolerance, "t = -1");
        }

        [Test]
        [Description("Asks for a time after the run ends and checks the ghost stays on the last sample.")]
        public void Position_AfterEnd_IsClampedToLastSample()
        {
            _adapter.Evaluate(5f, out var position, out _);

            GhostTestAssert.Near(_run.Samples[3].Position, position, GhostTestAssert.PositionTolerance, "t = 5");
        }
    }
}
