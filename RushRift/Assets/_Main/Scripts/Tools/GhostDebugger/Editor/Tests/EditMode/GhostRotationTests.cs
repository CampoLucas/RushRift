using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.Ghost
{
    /// <summary>
    /// Rotation interpolation and rotation compression accuracy.
    /// </summary>
    public class GhostRotationTests
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
            _adapter.Dispose();
        }

        [Test]
        public void Rotation_AtEveryStep_MatchesSlerp()
        {
            var run = GhostTestRuns.RotationRun();
            _adapter.LoadForPlayback(run);

            for (var k = 0; k <= 20; k++)
            {
                var segment = Mathf.Min(k / 10, 1);
                var fraction = (k - segment * 10) / 10f;
                var expected = Quaternion.Slerp(run.Samples[segment].Rotation, run.Samples[segment + 1].Rotation, fraction);

                _adapter.Evaluate(k * 0.01f, out _, out var rotation);

                GhostTestAssert.Near(expected, rotation, GhostTestAssert.RotationTolerance, $"t = {k * 0.01f:F2}");
            }
        }

        [Test]
        public void Rotation_AtMidpoint_IsHalfwayBetweenSamples()
        {
            _adapter.LoadForPlayback(GhostTestRuns.RotationRun());

            _adapter.Evaluate(0.05f, out _, out var rotation);

            GhostTestAssert.Near(Quaternion.Euler(0f, 45f, 0f), rotation, GhostTestAssert.RotationTolerance, "t = 0.05");
        }

        [Test]
        public void Rotation_AcrossWrapAround_TakesShortPath()
        {
            _adapter.LoadForPlayback(GhostTestRuns.ShortPathRun());

            _adapter.Evaluate(0.05f, out _, out var rotation);

            GhostTestAssert.Near(Quaternion.Euler(0f, 180f, 0f), rotation, GhostTestAssert.RotationTolerance, "t = 0.05");
        }

        [Test]
        public void Rotation_AfterCompressionRoundTrip_IsUnchanged()
        {
            var rotations = GhostTestRuns.CompressionRotations();

            for (var i = 0; i < rotations.Count; i++)
            {
                var result = _adapter.RoundTripRotation(rotations[i]);

                GhostTestAssert.Near(rotations[i], result, GhostTestAssert.RotationTolerance, $"rotation {i}");
            }
        }
    }
}
