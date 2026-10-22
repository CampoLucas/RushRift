using System;
using System.Linq;
using UnityEngine;

namespace Game.GhostDebugger.Tests
{
    /// <summary>
    /// The only bridge between the ghost tests and the real ghost code.
    /// </summary>
    public class GhostTestAdapter : IDisposable
    {
        private static Type _backendType;

        private readonly IGhostTestBackend _backend;

        public GhostTestRun RecordedRun => _backend.RecordedRun;
        public int SampleLimit => _backend.SampleLimit;
        public int CurrentVersion => _backend.CurrentVersion;
        public int FinishLayer => _backend.FinishLayer;
        public int CheckpointLayer => _backend.CheckpointLayer;
        public int PlayerLayer => _backend.PlayerLayer;

        public GhostTestAdapter()
        {
            _backend = (IGhostTestBackend)Activator.CreateInstance(FindBackendType());
        }

        public void SetRecordInterval(float interval) => _backend.SetRecordInterval(interval);
        public void SetTime(float time) => _backend.SetTime(time);
        public void BeginRecording() => _backend.BeginRecording();
        public void GiveControl(Vector3 position, Quaternion rotation) => _backend.GiveControl(position, rotation);
        public void Feed(Vector3 position, Quaternion rotation) => _backend.Feed(position, rotation);
        public void Pause() => _backend.Pause();
        public void Resume() => _backend.Resume();
        public void Finish(Vector3 position, Quaternion rotation) => _backend.Finish(position, rotation);
        public void LoadForPlayback(GhostTestRun run) => _backend.LoadForPlayback(run);
        public void Evaluate(float time, out Vector3 position, out Quaternion rotation) => _backend.Evaluate(time, out position, out rotation);
        public Quaternion RoundTripRotation(Quaternion rotation) => _backend.RoundTripRotation(rotation);
        public void Save(GhostTestRun run, string path) => _backend.Save(run, path);
        public GhostTestLoadResult Load(string path) => _backend.Load(path);
        public void WriteOutdatedVersionFile(string path) => _backend.WriteOutdatedVersionFile(path);
        public void ExportJson(GhostTestRun run, string path) => _backend.ExportJson(run, path);
        public GhostTestRun ImportJson(string path) => _backend.ImportJson(path);
        public void SetBestRunFolder(string folder) => _backend.SetBestRunFolder(folder);
        public bool SubmitRun(GhostTestRun run) => _backend.SubmitRun(run);
        public GhostTestLoadResult LoadBestRun() => _backend.LoadBestRun();
        public GameObject SpawnGhost(GhostTestRun run) => _backend.SpawnGhost(run);
        public void StartRace() => _backend.StartRace();
        public void PauseGame() => _backend.PauseGame();
        public void ResumeGame() => _backend.ResumeGame();

        public void Dispose()
        {
            _backend?.Dispose();
        }

        private static Type FindBackendType()
        {
            if (_backendType != null) return _backendType;

            _backendType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(SafeTypes)
                .FirstOrDefault(t => typeof(IGhostTestBackend).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

            if (_backendType == null)
            {
                throw new InvalidOperationException("No IGhostTestBackend implementation found. Expected GhostTestBackend in Tools/Editor/GhostDebugger.");
            }

            return _backendType;
        }

        private static Type[] SafeTypes(System.Reflection.Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (System.Reflection.ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null).ToArray();
            }
        }
    }
}
