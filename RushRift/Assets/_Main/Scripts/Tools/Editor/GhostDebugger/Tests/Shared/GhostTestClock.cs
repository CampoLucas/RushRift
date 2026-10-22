using System;
using UnityEngine;

namespace Game.GhostDebugger.Tests
{
    /// <summary>
    /// Fixed clock steps used to feed the recorder, always computed from an absolute step index.
    /// </summary>
    public static class GhostTestClock
    {
        public const float Step = 0.02f;
        public const float Interval = 0.1f;

        public static float TimeAt(int step)
        {
            return step * Step;
        }

        public static void Feed(GhostTestAdapter adapter, int fromStep, int toStep, Func<float, Vector3> position)
        {
            for (var step = fromStep; step <= toStep; step++)
            {
                var time = TimeAt(step);
                adapter.SetTime(time);
                adapter.Feed(position(time), Quaternion.identity);
            }
        }

        public static void StartRecording(GhostTestAdapter adapter, Vector3 startPosition)
        {
            adapter.SetRecordInterval(Interval);
            adapter.SetTime(0f);
            adapter.BeginRecording();
            adapter.GiveControl(startPosition, Quaternion.identity);
        }
    }
}
