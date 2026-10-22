using System.Collections.Generic;
using UnityEngine;

namespace Game.Tests.Ghost
{
    /// <summary>
    /// Known runs with fixed values used by the ghost tests.
    /// </summary>
    public static class GhostTestRuns
    {
        public static GhostTestRun RunA()
        {
            return Build(
                new GhostTestSample(0.0f, new Vector3(0f, 0f, 0f), Quaternion.identity),
                new GhostTestSample(0.1f, new Vector3(1f, 2f, 0f), Quaternion.identity),
                new GhostTestSample(0.2f, new Vector3(3f, 2f, -1f), Quaternion.identity),
                new GhostTestSample(0.3f, new Vector3(3f, 5f, -1f), Quaternion.identity));
        }

        public static GhostTestRun RotationRun()
        {
            return Build(
                new GhostTestSample(0.0f, Vector3.zero, Quaternion.Euler(0f, 0f, 0f)),
                new GhostTestSample(0.1f, Vector3.zero, Quaternion.Euler(0f, 90f, 0f)),
                new GhostTestSample(0.2f, Vector3.zero, Quaternion.Euler(0f, 90f, 45f)));
        }

        public static GhostTestRun ShortPathRun()
        {
            return Build(
                new GhostTestSample(0.0f, Vector3.zero, Quaternion.Euler(0f, 170f, 0f)),
                new GhostTestSample(0.1f, Vector3.zero, Quaternion.Euler(0f, -170f, 0f)));
        }

        public static GhostTestRun RandomRun(int seed, int count, float interval)
        {
            var random = new System.Random(seed);
            var samples = new GhostTestSample[count];
            for (var i = 0; i < count; i++)
            {
                var position = new Vector3(Range(random, -500f, 500f), Range(random, -500f, 500f), Range(random, -500f, 500f));
                samples[i] = new GhostTestSample(i * interval, position, RandomRotation(random));
            }

            return Build(samples);
        }

        public static GhostTestRun MarkedRun(float runTime, Vector3 marker)
        {
            var samples = new GhostTestSample[11];
            for (var i = 0; i < samples.Length; i++)
            {
                samples[i] = new GhostTestSample(runTime * i / 10f, marker + new Vector3(i, 0f, 0f), Quaternion.identity);
            }

            return Build(samples);
        }

        public static GhostTestRun StraightLine()
        {
            var samples = new GhostTestSample[21];
            for (var i = 0; i < samples.Length; i++)
            {
                samples[i] = new GhostTestSample(i * 0.1f, new Vector3(i, 0f, 0f), Quaternion.identity);
            }

            return Build(samples);
        }

        public static List<Quaternion> CompressionRotations()
        {
            var rotations = new List<Quaternion>
            {
                Quaternion.identity,
                Quaternion.Euler(30f, 45f, 60f),
                Quaternion.Euler(0f, 179.9f, 0f),
                Quaternion.Euler(-89f, 10f, 0f),
                Quaternion.Euler(89.9f, 0f, 0f)
            };

            var random = new System.Random(1234);
            for (var i = 0; i < 100; i++)
            {
                rotations.Add(RandomRotation(random));
            }

            return rotations;
        }

        private static GhostTestRun Build(params GhostTestSample[] samples)
        {
            return new GhostTestRun
            {
                Samples = new List<GhostTestSample>(samples),
                RunTime = samples[samples.Length - 1].Time,
                Finished = true
            };
        }

        private static float Range(System.Random random, float min, float max)
        {
            return (float)(min + random.NextDouble() * (max - min));
        }

        private static Quaternion RandomRotation(System.Random random)
        {
            return Quaternion.Euler(Range(random, 0f, 360f), Range(random, 0f, 360f), Range(random, 0f, 360f));
        }
    }
}
