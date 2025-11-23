using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Game;
using Game.DesignPatterns.Observers;
using Game.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Main.Scripts.Ghost
{
    [DisallowMultipleComponent]
    public class GhostRecorder : MonoBehaviour
    {
        [Header("Recording")]
        [SerializeField] private bool startRecordingOnEnable = true;
        [SerializeField] private bool recordAtFixedUpdate = true;
        [SerializeField] private float minFrameIntervalSeconds = 0.02f;
        [SerializeField] private float minPositionDeltaMeters = 0.005f;
        [SerializeField] private float minRotationDeltaDegrees = 0.5f;
        [SerializeField] private int maxRecordedFrames;

        [Header("Pause Integration")]
        [SerializeField] private bool obeyPauseEvents = true;

        [Header("Storage")]
        [SerializeField] private string ghostsFolderName = "ghosts";
        [SerializeField] private string fileNamePattern = "level_{LEVEL}.ghost.json";

        [Header("Debug UI")]
        [SerializeField] private bool showDebugUI = true;

        [Serializable]
        public struct GhostFrame { public float time; public Vector3 position; public Quaternion rotation; }

        [Serializable]
        public class GhostRunData
        {
            public int levelIndex;
            public float durationSeconds;
            public List<GhostFrame> frames = new List<GhostFrame>(1024);
            public string recordedAtUtc;
            public string appVersion;
        }

        private const float MinValidDurationSeconds = 0.25f;
        private const float Epsilon = 0.0005f;

        private GhostRunData currentRun;
        private bool isRecording;
        private bool pauseGate;
        private float lastFrameTime;
        private Vector3 lastPos;
        private Quaternion lastRot;
        
        private ActionObserver<bool> winObserver;
        private int levelIndex;

        private NullCheck<ActionObserver<bool>> _onPause;
        private NullCheck<Transform> _target;
        private NullCheck<ActionObserver> _onLevelReadySimple;

        private void Awake()
        {
            if (!_onPause) _onPause = new ActionObserver<bool>(OnPauseChanged);
            if (!_onLevelReadySimple) _onLevelReadySimple = new ActionObserver(OnLevelReady);
            
            levelIndex = SceneManager.GetActiveScene().buildIndex;
            winObserver = new ActionObserver<bool>(OnGameOverHandler);
            GlobalEvents.GameOver.Attach(winObserver);
        }

        private void OnEnable()
        {
            if (!_onPause) _onPause = new ActionObserver<bool>(OnPauseChanged);
            if (!_onLevelReadySimple) _onLevelReadySimple = new ActionObserver(OnLevelReady);

            PauseHandler.Attach(_onPause.Get());
            OnPauseChanged(PauseHandler.IsPaused);

            GameEntry.LoadingState.LevelChanged.Attach(_onLevelReadySimple.Get());

            if (startRecordingOnEnable)
            {
                StartCoroutine(WaitForPlayerAndStart());
            }
        }

        private void OnDisable()
        {
            if (_onPause) PauseHandler.Detach(_onPause.Get());
            if (_onLevelReadySimple) GameEntry.LoadingState.LevelChanged.Detach(_onLevelReadySimple.Get());
            
            GlobalEvents.GameOver.Detach(winObserver);
            
            StopRecording();
        }

        private void OnDestroy()
        {
            StopRecording();
            if (winObserver != null)
            {
                GlobalEvents.GameOver.Detach(winObserver);
                winObserver.Dispose();
                winObserver = null;
            }
        }
        
        private IEnumerator WaitForPlayerAndStart()
        {
            if (TryFindPlayer())
            {
                StartRecording();
                yield break;
            }

            float timeout = 10f;
            while (timeout > 0)
            {
                if (TryFindPlayer())
                {
                    StartRecording();
                    yield break;
                }

                timeout -= Time.deltaTime;
                yield return null;
            }
             Debug.LogWarning("[GhostRecorder] Player not found after timeout.");
        }

        private bool TryFindPlayer()
        {
            if (PlayerSpawner.Player.TryGet(out var p) && p != null)
            {
                _target = p.transform;
                return true;
            }

            var directFind = FindObjectOfType<PlayerController>();
            if (directFind != null)
            {
                _target = directFind.transform;
                return true;
            }

            return false;
        }

        private void Update()
        {
            if (!recordAtFixedUpdate) TickRecord(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (recordAtFixedUpdate) TickRecord(Time.fixedDeltaTime);
        }

        private void OnPauseChanged(bool paused)
        {
            if (!obeyPauseEvents) return;
            pauseGate = paused;
        }

        private void OnLevelReady()
        {
            Debug.Log("[GhostRecorder] Level Ready. Resetting state and Re-subscribing to events.");
            
            GlobalEvents.GameOver.Detach(winObserver);
            GlobalEvents.GameOver.Attach(winObserver);
            
            StopRecording();
            levelIndex = SceneManager.GetActiveScene().buildIndex;
            currentRun = null;
            lastFrameTime = 0f;
            _target.Reset(); 

            if (startRecordingOnEnable) 
                StartCoroutine(WaitForPlayerAndStart());
        }

        public void StartRecording()
        {
            if (!_target.TryGet(out var target)) return;
            
            currentRun = new GhostRunData
            {
                levelIndex = levelIndex,
                durationSeconds = 0f,
                recordedAtUtc = DateTime.UtcNow.ToString("o"),
                appVersion = Application.version
            };
            isRecording = true;
            lastFrameTime = 0f;
            lastPos = target.position;
            lastRot = target.rotation;
            PushFrame(0f, lastPos, lastRot);
            Debug.Log("[GhostRecorder] Started.");
        }

        public void StopRecording()
        {
            if (!isRecording) return;
            isRecording = false;
        }

        private void TickRecord(float dt)
        {
            if (!isRecording || !_target.TryGet(out var target)) return;
            if (obeyPauseEvents && (pauseGate || PauseHandler.IsPaused)) return;

            currentRun.durationSeconds += dt;
            float t = currentRun.durationSeconds;

            if (t - lastFrameTime < minFrameIntervalSeconds) return;

            Vector3 p = target.position;
            Quaternion r = target.rotation;

            if (Vector3.SqrMagnitude(p - lastPos) < minPositionDeltaMeters * minPositionDeltaMeters &&
                Quaternion.Angle(r, lastRot) < minRotationDeltaDegrees)
                return;

            PushFrame(t, p, r);
        }

        private void PushFrame(float time, Vector3 pos, Quaternion rot)
        {
            GhostFrame f; f.time = time; f.position = pos; f.rotation = rot;
            currentRun.frames.Add(f);
            lastFrameTime = time; lastPos = pos; lastRot = rot;

            if (maxRecordedFrames > 0 && currentRun.frames.Count > maxRecordedFrames)
                currentRun.frames.RemoveAt(0);
        }

        private void OnGameOverHandler(bool levelWon)
        {
            if (!levelWon) return; 

            float measuredDuration = currentRun != null ? currentRun.durationSeconds : 0f;
            
            if (measuredDuration <= MinValidDurationSeconds || currentRun == null || currentRun.frames == null || currentRun.frames.Count < 2)
            {
                Debug.LogWarning($"[GhostRecorder] Run too short to save: {measuredDuration}");
                StopRecording();
                return;
            }

            EnsureFolderExists();
            string path = GetFilePathForLevel(levelIndex);

            if (TryLoadBestGhostForLevel(levelIndex, out var existing, out _))
            {
                if (existing != null && existing.durationSeconds > MinValidDurationSeconds)
                {
                    if (measuredDuration + Epsilon < existing.durationSeconds)
                    {
                        Debug.Log($"[GhostRecorder] NEW BEST TIME! Saving to {path}");
                        AtomicSave(currentRun, path);
                    }
                    else
                    {
                        Debug.Log($"[GhostRecorder] No new record. Current: {measuredDuration} vs Best: {existing.durationSeconds}");
                    }
                }
                else
                {
                    AtomicSave(currentRun, path);
                }
            }
            else
            {
                Debug.Log($"[GhostRecorder] First ghost saved to {path}");
                AtomicSave(currentRun, path);
            }

            StopRecording();
        }

        private string GetFolderPath() => Path.Combine(Application.persistentDataPath, ghostsFolderName);
        private string GetFilePathForLevel(int idx) => Path.Combine(GetFolderPath(), fileNamePattern.Replace("{LEVEL}", idx.ToString()));

        private void EnsureFolderExists()
        {
            string folder = GetFolderPath();
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
        }

        private static bool TryReadJson(string path, out GhostRunData data)
        {
            try
            {
                data = JsonUtility.FromJson<GhostRunData>(File.ReadAllText(path));
                return data != null;
            }
            catch
            {
                data = null;
                return false;
            }
        }

        private void AtomicSave(GhostRunData run, string path)
        {
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(run));
            try { if (File.Exists(path)) File.Delete(path); } catch { }
            File.Move(tmp, path);
            Debug.Log($"[GhostRecorder] FILE SAVED.");
        }

        public static bool TryLoadBestGhostForCurrentLevel(out GhostRunData data, out string path)
        {
            int level = SceneManager.GetActiveScene().buildIndex;
            return TryLoadBestGhostForLevel(level, out data, out path);
        }

        public static bool TryLoadBestGhostForCurrentLevel(out GhostRunData data)
        {
            return TryLoadBestGhostForCurrentLevel(out data, out _);
        }

        public static bool TryLoadBestGhostForLevel(int levelIndex, out GhostRunData data, out string path)
        {
            path = Path.Combine(Application.persistentDataPath, "ghosts", $"level_{levelIndex}.ghost.json");
            if (File.Exists(path) && TryReadJson(path, out data))
                return data.levelIndex == levelIndex && data.frames != null && data.frames.Count >= 2 && data.durationSeconds > MinValidDurationSeconds;

            data = null;
            return false;
        }
        
        private void OnGUI()
        {
            if (!showDebugUI || !isRecording || currentRun == null) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 100));
            GUI.color = Color.red;
            GUILayout.Label($"[REC] TIME: {currentRun.durationSeconds:F2}");
            GUILayout.Label($"FRAMES: {currentRun.frames.Count}");
            GUI.color = Color.white;
            GUILayout.EndArea();
        }
    }
}