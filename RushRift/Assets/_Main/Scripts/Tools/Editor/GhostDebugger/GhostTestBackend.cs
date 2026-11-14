#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using _Main.Scripts.Ghost;
using Game.GhostDebugger.Tests;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.GhostDebugger
{
    /// <summary>
    /// Connects the ghost test adapter to GhostRecorder, GhostPlayer and GhostPlaybackUtils.
    /// </summary>
    public class GhostTestBackend : IGhostTestBackend
    {
        private const string TestLevelName = "GhostTest";
        private const string LoadLevelName = "GhostTestLoad";
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.NonPublic;

        private readonly string _ownFolder;
        private readonly string _previousFolderOverride;
        private readonly bool _previousPaused;
        private GhostRecorder _recorder;
        private Transform _target;
        private GhostPlayer _player;
        private List<GhostPlaybackUtils.Frame> _playbackFrames = new();
        private float _time;
        private float _lastFeedTime;
        private float _lastPlayerTime;

        public GhostTestRun RecordedRun
        {
            get
            {
                var data = EnsureRecorder().CurrentRun;
                return data != null ? ToRun(data) : new GhostTestRun();
            }
        }

        public int SampleLimit => new SerializedObject(EnsureRecorder()).FindProperty("maxRecordedFrames").intValue;
        public int CurrentVersion => GhostRecorder.CurrentFormatVersion;
        public int FinishLayer => 0;
        public int CheckpointLayer => 0;
        public int PlayerLayer => Mathf.Max(0, LayerMask.NameToLayer("Player"));

        public GhostTestBackend()
        {
            _previousFolderOverride = GhostRecorder.FolderOverride;
            _previousPaused = PauseHandler.IsPaused;
            _ownFolder = Path.Combine(Application.temporaryCachePath, "GhostTests", "Backend_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_ownFolder);
            GhostRecorder.FolderOverride = _ownFolder;
        }

        public void SetRecordInterval(float interval)
        {
            var serialized = new SerializedObject(EnsureRecorder());
            serialized.FindProperty("minFrameIntervalSeconds").floatValue = interval;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public void SetTime(float time)
        {
            _time = time;
            if (!_player) return;

            var dt = _time - _lastPlayerTime;
            _lastPlayerTime = _time;
            _player.Advance(dt);
        }

        public void BeginRecording()
        {
            EnsureRecorder();
            _lastFeedTime = _time;
        }

        public void GiveControl(Vector3 position, Quaternion rotation)
        {
            EnsureRecorder();
            _target.SetPositionAndRotation(position, rotation);
            Field("_targetSceneName").SetValue(_recorder, TestLevelName);
            _recorder.StartRecording(_target);
            _lastFeedTime = _time;
        }

        public void Feed(Vector3 position, Quaternion rotation)
        {
            EnsureRecorder();
            _target.SetPositionAndRotation(position, rotation);
            var dt = _time - _lastFeedTime;
            _lastFeedTime = _time;
            _recorder.Tick(dt);
        }

        public void Pause()
        {
            PauseHandler.Pause(true);
        }

        public void Resume()
        {
            PauseHandler.Pause(false);
        }

        public void Finish(Vector3 position, Quaternion rotation)
        {
            Feed(position, rotation);
            Method("OnGameOverHandler").Invoke(_recorder, new object[] { true });
        }

        public void LoadForPlayback(GhostTestRun run)
        {
            _playbackFrames = new List<GhostPlaybackUtils.Frame>(run.Samples.Count);
            foreach (var sample in run.Samples)
            {
                _playbackFrames.Add(new GhostPlaybackUtils.Frame { time = sample.Time, position = sample.Position, rotation = sample.Rotation });
            }
        }

        public void Evaluate(float time, out Vector3 position, out Quaternion rotation)
        {
            var i1 = GhostPlaybackUtils.AdvanceFrameIndex(_playbackFrames, time, 1);
            var i0 = i1 - 1;
            position = GhostPlaybackUtils.SamplePosition(_playbackFrames, i0, i1, time, GhostPlaybackUtils.PosInterp.Linear);
            rotation = GhostPlaybackUtils.SampleRotation(_playbackFrames, i0, i1, time, GhostPlaybackUtils.RotMode.UseRecordedRotation, position, position, 0f, 0f);
        }

        public Quaternion RoundTripRotation(Quaternion rotation)
        {
            var run = new GhostTestRun
            {
                Samples = new List<GhostTestSample>
                {
                    new(0f, Vector3.zero, rotation),
                    new(1f, Vector3.one, rotation)
                },
                RunTime = 1f,
                Version = CurrentVersion,
                Finished = true
            };

            var path = Path.Combine(_ownFolder, "rotation.ghost");
            Save(run, path);
            var result = Load(path);
            return result.Run != null && result.Run.Samples.Count > 0 ? result.Run.Samples[0].Rotation : Quaternion.identity;
        }

        public void Save(GhostTestRun run, string path)
        {
            Method("AtomicSave").Invoke(EnsureRecorder(), new object[] { ToData(run), path });
        }

        public GhostTestLoadResult Load(string path)
        {
            var folder = string.IsNullOrEmpty(GhostRecorder.FolderOverride) ? _ownFolder : GhostRecorder.FolderOverride;
            var copy = Path.Combine(folder, $"level_{LoadLevelName}.ghost.json");
            File.Copy(path, copy, true);

            try
            {
                var valid = GhostRecorder.TryLoadBestGhostForLevel(LoadLevelName, out var data, out _);
                return new GhostTestLoadResult { IsValid = valid, Run = data != null ? ToRun(data) : null };
            }
            finally
            {
                File.Delete(copy);
            }
        }

        public void WriteOutdatedVersionFile(string path)
        {
            var data = ToData(GhostTestRuns.StraightLine());
            data.formatVersion = GhostRecorder.MinFormatVersion - 1;
            File.WriteAllText(path, JsonUtility.ToJson(data));
        }

        public void ExportJson(GhostTestRun run, string path)
        {
            GhostRunJson.Export(ToData(run), path);
        }

        public GhostTestRun ImportJson(string path)
        {
            return ToRun(GhostRunJson.Import(path));
        }

        public void SetBestRunFolder(string folder)
        {
            GhostRecorder.FolderOverride = folder;
        }

        public bool SubmitRun(GhostTestRun run)
        {
            var recorder = EnsureRecorder();
            var path = Path.Combine(GhostRecorder.FolderOverride, $"level_{TestLevelName}.ghost.json");
            var before = File.Exists(path) ? File.ReadAllText(path) : null;

            Field("currentRun").SetValue(recorder, ToData(run));
            Field("isRecording").SetValue(recorder, true);
            Field("_targetSceneName").SetValue(recorder, TestLevelName);
            Method("OnGameOverHandler").Invoke(recorder, new object[] { run.Finished });

            var after = File.Exists(path) ? File.ReadAllText(path) : null;
            return after != null && after != before;
        }

        public GhostTestLoadResult LoadBestRun()
        {
            var valid = GhostRecorder.TryLoadBestGhostForLevel(TestLevelName, out var data, out _);
            return new GhostTestLoadResult { IsValid = valid, Run = data != null ? ToRun(data) : null };
        }

        public GameObject SpawnGhost(GhostTestRun run)
        {
            var go = new GameObject("GhostTestPlayer");
            go.SetActive(false);
            _player = go.AddComponent<GhostPlayer>();

            var serialized = new SerializedObject(_player);
            serialized.FindProperty("autoLoadOnEnable").boolValue = false;
            serialized.FindProperty("beginPlaybackOnEnable").boolValue = false;
            serialized.FindProperty("alignGhostStartToPlayer").boolValue = false;
            serialized.FindProperty("enableProximityFade").boolValue = false;
            serialized.FindProperty("applyOutputSmoothing").boolValue = false;
            serialized.FindProperty("positionInterpolation").intValue = (int)GhostPlayer.PositionInterpolationMode.Linear;
            serialized.FindProperty("toggleVisibilityKey").intValue = (int)KeyCode.None;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            go.SetActive(true);
            _player.ExternalTimeControl = true;
            _player.LoadRun(ToData(run), "GhostTestBackend");
            _lastPlayerTime = _time;
            return _player.GhostTransform.gameObject;
        }

        public void StartRace()
        {
            _player.Play();
        }

        public void PauseGame()
        {
            PauseHandler.Pause(true);
        }

        public void ResumeGame()
        {
            PauseHandler.Pause(false);
        }

        public void Dispose()
        {
            if (_player) DestroyObject(_player.gameObject);
            if (_recorder) DestroyObject(_recorder.gameObject);
            if (_target) DestroyObject(_target.gameObject);
            PauseHandler.Pause(_previousPaused);
            GhostRecorder.FolderOverride = _previousFolderOverride;
            if (Directory.Exists(_ownFolder)) Directory.Delete(_ownFolder, true);
        }

        private GhostRecorder EnsureRecorder()
        {
            if (_recorder) return _recorder;

            var go = new GameObject("GhostTestRecorder") { hideFlags = HideFlags.HideAndDontSave };
            go.SetActive(false);
            _recorder = go.AddComponent<GhostRecorder>();
            _recorder.ExternalTimeControl = true;

            _target = new GameObject("GhostTestTarget") { hideFlags = HideFlags.HideAndDontSave }.transform;
            return _recorder;
        }

        private static GhostRecorder.GhostRunData ToData(GhostTestRun run)
        {
            var data = new GhostRecorder.GhostRunData
            {
                levelName = TestLevelName,
                durationSeconds = run.RunTime,
                recordedAtUtc = DateTime.UtcNow.ToString("o"),
                appVersion = Application.version,
                formatVersion = run.Version
            };

            foreach (var sample in run.Samples)
            {
                data.frames.Add(new GhostRecorder.GhostFrame { time = sample.Time, position = sample.Position, rotation = sample.Rotation });
            }

            return data;
        }

        private static GhostTestRun ToRun(GhostRecorder.GhostRunData data)
        {
            var run = new GhostTestRun
            {
                RunTime = data.durationSeconds,
                Version = data.formatVersion,
                Finished = true
            };

            if (data.frames == null) return run;

            foreach (var frame in data.frames)
            {
                run.Samples.Add(new GhostTestSample(frame.time, frame.position, frame.rotation));
            }

            return run;
        }

        private static FieldInfo Field(string name)
        {
            return typeof(GhostRecorder).GetField(name, InstanceFlags) ?? throw new MissingFieldException(nameof(GhostRecorder), name);
        }

        private static MethodInfo Method(string name)
        {
            return typeof(GhostRecorder).GetMethod(name, InstanceFlags | StaticFlags) ?? throw new MissingMethodException(nameof(GhostRecorder), name);
        }

        private static void DestroyObject(Object target)
        {
            if (Application.isPlaying) Object.Destroy(target);
            else Object.DestroyImmediate(target);
        }
    }
}
#endif
