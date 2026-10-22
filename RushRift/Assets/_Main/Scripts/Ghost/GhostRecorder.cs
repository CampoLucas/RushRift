using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Game;
using Game.DesignPatterns.Observers;
using Game.Entities;
using Game.Levels;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Main.Scripts.Ghost
{
    [DisallowMultipleComponent]
    public class GhostRecorder : MonoBehaviour
    {
        [Header("Recording Settings")]
        [SerializeField] private bool startRecordingOnEnable = true;
        [SerializeField] private bool recordAtFixedUpdate = true;
        [SerializeField] private float minFrameIntervalSeconds = 0.02f;
        [SerializeField] private float minPositionDeltaMeters = 0.005f;
        [SerializeField] private float minRotationDeltaDegrees = 0.5f;
        [SerializeField] private int maxRecordedFrames;

        [Header("Scene Filtering")]
        [Tooltip("Exact names of scenes to record. If empty, records Active Scene.")]
        [SerializeField] private List<string> allowedScenes = new List<string>(); 

        [Header("Pause Integration")]
        [SerializeField] private bool obeyPauseEvents = true;

        [Header("Storage")]
        [SerializeField] private string ghostsFolderName = "ghosts";
        // UPDATED PATTERN: Now implies we will put the NAME here
        [SerializeField] private string fileNamePattern = "level_{NAME}.ghost.json";

        [Header("Debug UI")]
        [SerializeField] private bool showDebugUI = true;

        [Serializable]
        public struct GhostFrame { public float time; public Vector3 position; public Quaternion rotation; }

        [Serializable]
        public class GhostRunData
        {
            public string levelName; // Changed from index to name
            public float durationSeconds;
            public List<GhostFrame> frames = new List<GhostFrame>(1024);
            public string recordedAtUtc;
            public string appVersion;
            public int formatVersion;
        }

        public const int CurrentFormatVersion = 1;
        public const int MinFormatVersion = 0;

        private const float MinValidDurationSeconds = 0.25f;
        private const float Epsilon = 0.0005f;

        private GhostRunData currentRun;
        private bool isRecording;
        private bool pauseGate;
        private float lastFrameTime;
        private Vector3 lastPos;
        private Quaternion lastRot;
        
        private ActionObserver<bool> winObserver;
        
        // We use this to store the specific scene NAME we decided to record
        private string _targetSceneName = ""; 

        private NullCheck<ActionObserver<bool>> _onPause;
        private NullCheck<Transform> _target;
        private ActionObserver<BaseLevelSO> _onLevelReady;
        private ActionObserver<bool> _onLoading;

        private void Awake()
        {
            if (!_onPause) _onPause = new ActionObserver<bool>(OnPauseChanged);
            _onLevelReady = new ActionObserver<BaseLevelSO>(_ => OnLevelReady());
            _onLoading = new ActionObserver<bool>(OnLoadingChanged);
            
            winObserver = new ActionObserver<bool>(OnGameOverHandler);
            GlobalEvents.GameOver.Attach(winObserver);
        }

        private void OnEnable()
        {
            if (!_onPause) _onPause = new ActionObserver<bool>(OnPauseChanged);

            PauseHandler.Attach(_onPause.Get());
            OnPauseChanged(PauseHandler.IsPaused);

            GameEntry.LoadingState.AttachOnReady(_onLevelReady);
            GameEntry.LoadingState.AttachOnLoading(_onLoading);
            GlobalEvents.GameOver.Attach(winObserver);

            if (startRecordingOnEnable)
            {
                CheckSceneAndStart();
            }
        }

        private void OnDisable()
        {
            if (_onPause) PauseHandler.Detach(_onPause.Get());
            GameEntry.LoadingState.DetachOnReady(_onLevelReady);
            GameEntry.LoadingState.DetachOnLoading(_onLoading);
            StopAllCoroutines();
            
            GlobalEvents.GameOver.Detach(winObserver);
            StopRecording();
        }

        private void Start()
        {
            // GlobalLevelManager may reset the event subscriptions after our Awake.
            GlobalEvents.GameOver.Attach(winObserver);
        }

        private void OnLoadingChanged(bool loading)
        {
            if (!loading) return;
            StopAllCoroutines();
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
        
        private void CheckSceneAndStart()
        {
            if (GameEntry.LoadingState.Loading) return;
            if (TryGetAllowedSceneName(out string sceneName))
            {
                Debug.Log($"[GhostRecorder] Scene '{sceneName}' is allowed. Initializing...");
                _targetSceneName = sceneName; 
                StartCoroutine(WaitForPlayerAndStart());
            }
        }

        private bool TryGetAllowedSceneName(out string name)
        {
            name = "";

            // 1. If list is empty, default to whatever is Active
            if (allowedScenes == null || allowedScenes.Count == 0)
            {
                name = SceneManager.GetActiveScene().name;
                return true;
            }

            // 2. Iterate over ALL loaded scenes to find the one that matches our Allowed List
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (allowedScenes.Contains(s.name))
                {
                    name = s.name;
                    return true; 
                }
            }

            return false;
        }
        
        private IEnumerator WaitForPlayerAndStart()
        {
            if (TryFindPlayer()) { StartRecording(); yield break; }

            float timeout = 10f;
            while (timeout > 0)
            {
                if (TryFindPlayer()) { StartRecording(); yield break; }
                timeout -= Time.unscaledDeltaTime;
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
#if UNITY_EDITOR
            if (ExternalTimeControl) return;
#endif
            if (!recordAtFixedUpdate) TickRecord(Time.deltaTime);
        }

        private void FixedUpdate()
        {
#if UNITY_EDITOR
            if (ExternalTimeControl) return;
#endif
            if (recordAtFixedUpdate) TickRecord(Time.fixedDeltaTime);
        }

#if UNITY_EDITOR
        public static string FolderOverride { get; set; }
        public bool ExternalTimeControl { get; set; }
        public GhostRunData CurrentRun => currentRun;

        public void Tick(float dt) => TickRecord(dt);

        public void StartRecording(Transform target)
        {
            _target = target;
            StartRecording();
        }
#endif

        private void OnPauseChanged(bool paused)
        {
            if (!obeyPauseEvents) return;
            pauseGate = paused;
        }

        private void OnLevelReady()
        {
            StopAllCoroutines();
            GlobalEvents.GameOver.Detach(winObserver); 
            GlobalEvents.GameOver.Attach(winObserver); 

            StopRecording();
            currentRun = null;
            lastFrameTime = 0f;
            _target.Reset(); 
            _targetSceneName = ""; 

            if (startRecordingOnEnable) 
            {
                CheckSceneAndStart();
            }
        }

        public void StartRecording()
        {
            if (!_target.TryGet(out var target)) return;
            
            if (string.IsNullOrEmpty(_targetSceneName)) 
                _targetSceneName = SceneManager.GetActiveScene().name;

            currentRun = new GhostRunData
            {
                levelName = _targetSceneName,
                durationSeconds = 0f,
                recordedAtUtc = DateTime.UtcNow.ToString("o"),
                appVersion = Application.version,
                formatVersion = CurrentFormatVersion
            };
            isRecording = true;
            lastFrameTime = 0f;
            lastPos = target.position;
            lastRot = target.rotation;
            PushFrame(0f, lastPos, lastRot);
            
            Debug.Log($"[GhostRecorder] Started recording for Level: {_targetSceneName}");
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
            if (!isRecording) return;
            StopRecording();
            if (!levelWon) return; 

            float measuredDuration = currentRun != null ? currentRun.durationSeconds : 0f;
            
            if (measuredDuration <= MinValidDurationSeconds || currentRun == null || currentRun.frames == null || currentRun.frames.Count < 2)
            {
                StopRecording();
                return;
            }

            EnsureFolderExists();
            string path = GetFilePathForLevel(_targetSceneName);

            if (TryLoadBestGhostForLevel(_targetSceneName, out var existing, out _))
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

        private string GetFolderPath()
        {
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(FolderOverride)) return FolderOverride;
#endif
            return Path.Combine(Application.persistentDataPath, ghostsFolderName);
        }
        
        // FIX: Path generation uses NAME now
        private string GetFilePathForLevel(string levelName) 
        {
            // Fallback if name is empty
            if (string.IsNullOrEmpty(levelName)) levelName = "UnknownLevel";
            return Path.Combine(GetFolderPath(), fileNamePattern.Replace("{LEVEL}", levelName).Replace("{NAME}", levelName));
        }

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

        public static bool TryLoadBestGhostForLevel(string levelName, out GhostRunData data, out string path)
        {
            string folder = Path.Combine(Application.persistentDataPath, "ghosts");
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(FolderOverride)) folder = FolderOverride;
#endif
            path = Path.Combine(folder, $"level_{levelName}.ghost.json");

            if (File.Exists(path) && TryReadJson(path, out data))
            {
                if (data.formatVersion < MinFormatVersion || data.formatVersion > CurrentFormatVersion)
                {
                    Debug.LogError($"[GhostRecorder] Unsupported ghost format version {data.formatVersion} in {path}");
                    data = null;
                    return false;
                }
                return data.frames != null && data.frames.Count >= 2 && data.durationSeconds > MinValidDurationSeconds;
            }

            data = null;
            return false;
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!showDebugUI || !isRecording || currentRun == null) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 400, 100));
            GUI.color = Color.red;
            GUILayout.Label($"[REC] Level: {_targetSceneName} | Time: {currentRun.durationSeconds:F2}");
            GUILayout.Label($"FRAMES: {currentRun.frames.Count}");
            GUI.color = Color.white;
            GUILayout.EndArea();
        }
#endif
    }
}
