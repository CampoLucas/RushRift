using System.Collections.Generic;

namespace Game.Tests.Ghost
{
    /// <summary>
    /// A whole ghost run as the tests see it, independent of the real ghost format.
    /// </summary>
    public class GhostTestRun
    {
        public List<GhostTestSample> Samples { get; set; } = new();
        public float RunTime { get; set; }
        public int Version { get; set; }
        public bool Finished { get; set; }
    }
}
