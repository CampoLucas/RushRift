using UnityEngine;

namespace Game.Tests.Ghost
{
    /// <summary>
    /// One recorded ghost frame as the tests see it.
    /// </summary>
    public struct GhostTestSample
    {
        public float Time { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }

        public GhostTestSample(float time, Vector3 position, Quaternion rotation)
        {
            Time = time;
            Position = position;
            Rotation = rotation;
        }
    }
}
