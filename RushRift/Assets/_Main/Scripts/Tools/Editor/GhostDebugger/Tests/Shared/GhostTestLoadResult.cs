namespace Game.GhostDebugger.Tests
{
    /// <summary>
    /// Result of loading a ghost file through the adapter.
    /// </summary>
    public class GhostTestLoadResult
    {
        public bool IsValid { get; set; }
        public GhostTestRun Run { get; set; }
    }
}
