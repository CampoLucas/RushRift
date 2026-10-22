using System;
using UnityEngine;

namespace Game.GhostDebugger.Tests
{
    /// <summary>
    /// Counts log messages by type while it is alive.
    /// </summary>
    public class GhostTestLogCapture : IDisposable
    {
        public int Errors { get; private set; }
        public int Exceptions { get; private set; }

        public GhostTestLogCapture()
        {
            Application.logMessageReceived += OnLog;
        }

        public void Dispose()
        {
            Application.logMessageReceived -= OnLog;
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error)
            {
                Errors++;
            }
            else if (type == LogType.Exception)
            {
                Exceptions++;
            }
        }
    }
}
