using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.DesignPatterns.Observers;

[DisallowMultipleComponent]
public class GhostRecorder : MonoBehaviour
{
    [Header("Target")]
    [SerializeField, Tooltip("Transform to record. If null, tries to find an object tagged Player.")]
    private Transform targetToRecord;

    [Header("Recording")]
    [SerializeField, Tooltip("Begin recording automatically on OnEnable.")]
    private bool startRecordingOnEnable = true;
    [SerializeField, Tooltip("Record at FixedUpdate for physics-aligned playback.")]
    private bool recordAtFixedUpdate = true;
    [SerializeField, Tooltip("Minimum time between frames in seconds.")]
    private float minFrameIntervalSeconds = 0.02f;
    [SerializeField, Tooltip("Do not record frames if the target moved less than this distance (meters).")]
    private float minPositionDeltaMeters = 0.005f;
    [SerializeField, Tooltip("Do not record frames if the target rotated less than this angle (degrees).")]
    private float minRotationDeltaDegrees = 0.5f;
    [SerializeField, Tooltip("Maximum number of frames to keep (0 = unlimited).")]
    private int maxRecordedFrames = 0;

    [Header("Storage")]
    [SerializeField, Tooltip("Directory under persistentDataPath where ghosts are saved.")]
    private string ghostsFolderName = "ghosts";
    [SerializeField, Tooltip("File pattern per level. {LEVEL} is replaced by the buildIndex.")]
    private string fileNamePattern = "level_{LEVEL}.ghost.json";

    [Header("Debug")]
    [SerializeField, Tooltip("If enabled, prints detailed logs.")]
    private bool isDebugLoggingEnabled = false;
    [SerializeField, Tooltip("Draw gizmos for a portion of the recorded path.")]
    private bool drawGizmos = true;
    [SerializeField, Tooltip("How many recent segments to draw with gizmos.")]
    private int gizmoSegments = 64;
    [SerializeField, Tooltip("Gizmo color for the path.")]
    private Color gizmoColor = new Color(0f, 1f, 0.6f, 0.9f);

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
    private float lastFrameTime;
    private Vector3 lastPos;
    private Quaternion lastRot;
    private IObserver winObserver;
    private int levelIndex;

    private void Awake()
    {
        levelIndex = SceneManager.GetActiveScene().buildIndex;

        if (!targetToRecord)
        {
            var tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged) targetToRecord = tagged.transform;
        }

        winObserver = new ActionObserver(OnLevelWon);
        WinTrigger.OnWinSaveTimes.Attach(winObserver);

        if (startRecordingOnEnable) StartRecording();
        Log("Recorder Awake");
    }

    private void OnEnable()
    {
        if (startRecordingOnEnable && !isRecording) StartRecording();
    }

    private void OnDisable()
    {
        StopRecording();
    }

    private void OnDestroy()
    {
        StopRecording();
        if (winObserver != null)
        {
            WinTrigger.OnWinSaveTimes.Detach(winObserver);
            winObserver.Dispose();
            winObserver = null;
        }
    }

    private void Update()
    {
        if (!recordAtFixedUpdate) TickRecord(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        if (recordAtFixedUpdate) TickRecord(Time.fixedDeltaTime);
    }

    public void StartRecording()
    {
        if (!targetToRecord) return;
        currentRun = new GhostRunData
        {
            levelIndex = levelIndex,
            durationSeconds = 0f,
            recordedAtUtc = DateTime.UtcNow.ToString("o"),
            appVersion = Application.version
        };
        isRecording = true;
        lastFrameTime = 0f;
        lastPos = targetToRecord.position;
        lastRot = targetToRecord.rotation;
        PushFrame(0f, lastPos, lastRot);
        Log("Recording started");
    }

    public void StopRecording()
    {
        if (!isRecording) return;
        isRecording = false;
        Log("Recording stopped");
    }

    private void TickRecord(float dt)
    {
        if (!isRecording || !targetToRecord) return;

        currentRun.durationSeconds += dt;
        float t = currentRun.durationSeconds;

        if (t - lastFrameTime < minFrameIntervalSeconds) return;

        Vector3 p = targetToRecord.position;
        Quaternion r = targetToRecord.rotation;

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

    private void OnLevelWon()
    {
        // Use the recorder’s measured duration; fall back to LevelManager only if needed
        float measuredDuration = currentRun != null ? currentRun.durationSeconds : 0f;
        if (measuredDuration <= MinValidDurationSeconds || currentRun == null || currentRun.frames == null || currentRun.frames.Count < 2)
        {
            Log($"Skip save: invalid run (t={measuredDuration:0.###}s, frames={currentRun?.frames?.Count ?? 0})");
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
                    AtomicSave(currentRun, path);
                    Log($"Saved BEST ({measuredDuration:0.###}s) → {path} (prev {existing.durationSeconds:0.###}s)");
                }
                else
                {
                    Log($"Kept previous BEST ({existing.durationSeconds:0.###}s) → {path}");
                }
            }
            else
            {
                AtomicSave(currentRun, path);
                Log($"Saved BEST ({measuredDuration:0.###}s) → {path} (prev invalid)");
            }
        }
        else
        {
            AtomicSave(currentRun, path);
            Log($"Saved FIRST BEST ({measuredDuration:0.###}s) → {path}");
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
        if (run == null || run.frames == null || run.frames.Count < 2 || run.durationSeconds <= MinValidDurationSeconds) return;

        string tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonUtility.ToJson(run));
        // Replace existing safely
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch { /* ignore */ }
        File.Move(tmp, path);
    }

    public static bool TryLoadBestGhostForCurrentLevel(out GhostRunData data, out string path)
    {
        int level = SceneManager.GetActiveScene().buildIndex;
        return TryLoadBestGhostForLevel(level, out data, out path);
    }

    public static bool TryLoadBestGhostForCurrentLevel(out GhostRunData data)
    {
        string _;
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

    private void Log(string msg)
    {
        if (!isDebugLoggingEnabled) return;
        Debug.Log($"[GhostRecorder] {name}: {msg}", this);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        if (currentRun == null || currentRun.frames == null || currentRun.frames.Count < 2) return;
        Gizmos.color = gizmoColor;
        int count = currentRun.frames.Count;
        int start = Mathf.Max(0, count - Mathf.Max(2, gizmoSegments));
        for (int i = start + 1; i < count; i++)
            Gizmos.DrawLine(currentRun.frames[i - 1].position, currentRun.frames[i].position);
    }
#endif
}