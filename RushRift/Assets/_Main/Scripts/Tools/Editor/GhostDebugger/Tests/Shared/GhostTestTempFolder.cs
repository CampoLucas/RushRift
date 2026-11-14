using System;
using System.IO;
using UnityEngine;

namespace Game.GhostDebugger.Tests
{
    /// <summary>
    /// Unique temporary folder for file tests, deleted on dispose.
    /// </summary>
    public class GhostTestTempFolder : IDisposable
    {
        public string Path { get; private set; }

        public GhostTestTempFolder()
        {
            Path = System.IO.Path.Combine(Application.temporaryCachePath, "GhostTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string File(string name)
        {
            return System.IO.Path.Combine(Path, name);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, true);
            }
        }
    }
}
