using System;
using UnityEngine;

namespace Game.GhostDebugger.Tests
{
    /// <summary>
    /// Implemented next to the ghost code so the test adapter can reach it without referencing Assembly-CSharp.
    /// </summary>
    public interface IGhostTestBackend : IDisposable
    {
        GhostTestRun RecordedRun { get; }
        int SampleLimit { get; }
        int CurrentVersion { get; }
        int FinishLayer { get; }
        int CheckpointLayer { get; }
        int PlayerLayer { get; }

        void SetRecordInterval(float interval);
        void SetTime(float time);
        void BeginRecording();
        void GiveControl(Vector3 position, Quaternion rotation);
        void Feed(Vector3 position, Quaternion rotation);
        void Pause();
        void Resume();
        void Finish(Vector3 position, Quaternion rotation);
        void LoadForPlayback(GhostTestRun run);
        void Evaluate(float time, out Vector3 position, out Quaternion rotation);
        Quaternion RoundTripRotation(Quaternion rotation);
        void Save(GhostTestRun run, string path);
        GhostTestLoadResult Load(string path);
        void WriteOutdatedVersionFile(string path);
        void ExportJson(GhostTestRun run, string path);
        GhostTestRun ImportJson(string path);
        void SetBestRunFolder(string folder);
        bool SubmitRun(GhostTestRun run);
        GhostTestLoadResult LoadBestRun();
        GameObject SpawnGhost(GhostTestRun run);
        void StartRace();
        void PauseGame();
        void ResumeGame();
    }
}
