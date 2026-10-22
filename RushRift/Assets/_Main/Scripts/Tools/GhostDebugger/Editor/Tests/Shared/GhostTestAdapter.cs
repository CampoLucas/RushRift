using System;
using UnityEngine;

namespace Game.Tests.Ghost
{
    /// <summary>
    /// The only bridge between the ghost tests and the real ghost code.
    /// </summary>
    public class GhostTestAdapter : IDisposable
    {
        public GhostTestRun RecordedRun => throw new NotImplementedException();
        public int SampleLimit => throw new NotImplementedException();
        public int CurrentVersion => throw new NotImplementedException();
        public int FinishLayer => throw new NotImplementedException();
        public int CheckpointLayer => throw new NotImplementedException();
        public int PlayerLayer => throw new NotImplementedException();

        public void SetRecordInterval(float interval)
        {
            throw new NotImplementedException();
        }

        public void SetTime(float time)
        {
            throw new NotImplementedException();
        }

        public void BeginRecording()
        {
            throw new NotImplementedException();
        }

        public void GiveControl(Vector3 position, Quaternion rotation)
        {
            throw new NotImplementedException();
        }

        public void Feed(Vector3 position, Quaternion rotation)
        {
            throw new NotImplementedException();
        }

        public void Pause()
        {
            throw new NotImplementedException();
        }

        public void Resume()
        {
            throw new NotImplementedException();
        }

        public void ReachCheckpoint()
        {
            throw new NotImplementedException();
        }

        public void ResetToCheckpoint()
        {
            throw new NotImplementedException();
        }

        public void Finish(Vector3 position, Quaternion rotation)
        {
            throw new NotImplementedException();
        }

        public void LoadForPlayback(GhostTestRun run)
        {
            throw new NotImplementedException();
        }

        public void Evaluate(float time, out Vector3 position, out Quaternion rotation)
        {
            throw new NotImplementedException();
        }

        public Quaternion RoundTripRotation(Quaternion rotation)
        {
            throw new NotImplementedException();
        }

        public void Save(GhostTestRun run, string path)
        {
            throw new NotImplementedException();
        }

        public GhostTestLoadResult Load(string path)
        {
            throw new NotImplementedException();
        }

        public void WriteOutdatedVersionFile(string path)
        {
            throw new NotImplementedException();
        }

        public void ExportJson(GhostTestRun run, string path)
        {
            throw new NotImplementedException();
        }

        public GhostTestRun ImportJson(string path)
        {
            throw new NotImplementedException();
        }

        public void SetBestRunFolder(string folder)
        {
            throw new NotImplementedException();
        }

        public bool SubmitRun(GhostTestRun run)
        {
            throw new NotImplementedException();
        }

        public GhostTestLoadResult LoadBestRun()
        {
            throw new NotImplementedException();
        }

        public GameObject SpawnGhost(GhostTestRun run)
        {
            throw new NotImplementedException();
        }

        public void StartRace()
        {
            throw new NotImplementedException();
        }

        public void PauseGame()
        {
            throw new NotImplementedException();
        }

        public void ResumeGame()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }
    }
}
