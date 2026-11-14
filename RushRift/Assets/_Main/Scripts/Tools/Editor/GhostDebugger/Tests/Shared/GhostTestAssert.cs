using NUnit.Framework;
using UnityEngine;

namespace Game.GhostDebugger.Tests
{
    /// <summary>
    /// Distance and angle assertions with explicit tolerances.
    /// </summary>
    public static class GhostTestAssert
    {
        public const float PositionTolerance = 0.001f;
        public const float RotationTolerance = 0.1f;
        public const float TimeTolerance = 0.0001f;

        public static void Near(Vector3 expected, Vector3 actual, float tolerance, string context)
        {
            var distance = Vector3.Distance(expected, actual);
            Assert.LessOrEqual(distance, tolerance, $"{context}: expected {expected:F4}, got {actual:F4}, distance {distance:F6}");
        }

        public static void Near(Quaternion expected, Quaternion actual, float toleranceDegrees, string context)
        {
            var angle = Quaternion.Angle(expected, actual);
            Assert.LessOrEqual(angle, toleranceDegrees, $"{context}: expected {expected.eulerAngles:F3}, got {actual.eulerAngles:F3}, angle {angle:F4}");
        }

        public static void Near(float expected, float actual, float tolerance, string context)
        {
            Assert.AreEqual(expected, actual, tolerance, context);
        }
    }
}
