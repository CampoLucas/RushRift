using System;
using System.Collections.Generic;

namespace Game.LeaderboardDebugger
{
    public enum ScoreDistribution
    {
        Uniform,
        Normal,
        LongTail
    }

    public static class LeaderboardGenerator
    {
        public static readonly float[] EdgeTimes =
        {
            0f,
            -1f,
            -0.001f,
            0.1f,
            0.3f,
            1.0000001f,
            123456.789f,
            16777217f,
            float.Epsilon,
            float.MaxValue,
            float.MinValue,
            float.PositiveInfinity,
            float.NegativeInfinity,
            float.NaN
        };

        public static readonly string[] EdgeNames =
        {
            "",
            " ",
            "Ünïcödé Ñame",
            "日本語のなまえ",
            "مرحبا",
            "😀🔥🚀",
            "O'Brien \"Quote\"",
            "back\\slash/forward",
            "line\nbreak\ttab",
            "<b>rich</b>",
            "null\0char"
        };

        private static readonly string[] FirstParts =
        {
            "Rift", "Dash", "Blink", "Neon", "Turbo", "Echo", "Pixel", "Nova", "Vortex", "Zero", "Ghost", "Flux", "Comet", "Hyper", "Shadow"
        };

        private static readonly string[] SecondParts =
        {
            "Runner", "Pilot", "Fox", "Wolf", "Storm", "Spark", "Blade", "Rider", "Hawk", "Bolt", "Cat", "Ninja", "Fang", "Wing", "Drift"
        };

        public static string RandomName(Random random)
        {
            var name = FirstParts[random.Next(FirstParts.Length)] + SecondParts[random.Next(SecondParts.Length)];
            return random.Next(2) == 0 ? name : name + random.Next(0, 100);
        }

        public static string RandomId(Random random)
        {
            var bytes = new byte[16];
            random.NextBytes(bytes);
            return new Guid(bytes).ToString();
        }

        public static float RandomTime(Random random, ScoreDistribution distribution, float min, float max)
        {
            var range = max - min;
            double value;

            switch (distribution)
            {
                case ScoreDistribution.Normal:
                    var u1 = 1.0 - random.NextDouble();
                    var u2 = random.NextDouble();
                    var gaussian = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
                    value = min + range / 2.0 + gaussian * range / 6.0;
                    break;
                case ScoreDistribution.LongTail:
                    value = min - Math.Log(1.0 - random.NextDouble()) * range / 8.0;
                    break;
                default:
                    value = min + random.NextDouble() * range;
                    break;
            }

            return (float)Math.Round(Math.Min(Math.Max(value, min), max), 3);
        }

        public static List<Score> Generate(int count, ScoreDistribution distribution, float min, float max, Random random = null)
        {
            random ??= new Random();
            var entries = new List<Score>(count);

            for (var i = 0; i < count; i++)
            {
                entries.Add(new Score
                {
                    name = RandomName(random),
                    id = RandomId(random),
                    timescore = RandomTime(random, distribution, min, max)
                });
            }

            return entries;
        }

        public static List<Score> CreateTies(int count, float time, Random random = null)
        {
            random ??= new Random();
            var entries = new List<Score>(count);

            for (var i = 0; i < count; i++)
            {
                entries.Add(new Score
                {
                    name = RandomName(random),
                    id = RandomId(random),
                    timescore = time
                });
            }

            return entries;
        }
    }
}
