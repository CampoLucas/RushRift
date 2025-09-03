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

    [Header("Save Policy")]
    [SerializeField, Tooltip("Save the BEST ghost only when a new personal best is set. LAST is always updated.")]
    private bool requireNewRecordToSave = true;
    [SerializeField, Tooltip("Require that the run meets the Bronze threshold (from LevelMedalsSO) to save BEST.")]
    private bool requireBronzeToSave = true;

    [Header("Storage")]
    [SerializeField, Tooltip("Directory under persistentDataPath where ghosts are saved.")]
    private string ghostsFolderName = "ghosts";
    [SerializeField, Tooltip("BEST file pattern per level. {LEVEL} is replaced by the buildIndex.")]
    private string bestFileNamePattern = "level_{LEVEL}.ghost.json";
    [SerializeField, Tooltip("LAST file pattern per level. {LEVEL} is replaced by the buildIndex.")]
    private string lastFileNamePattern = "last_level_{LEVEL}.ghost.json";
    [SerializeField, Tooltip("Also save the LAST run file every time you finish.")]
    private bool alsoSaveLastRun = true;

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

    private GhostRunData currentRun;
    private bool isRecording;
    private float lastFrameTime;
    private Vector3 lastPos;
    private Quaternion lastRot;
    private IObserver winObserver;

    private int levelIndex;
    private float bestTimeAtLevelStart;
    private bool bronzeAlreadyAcquired;
    private float bronzeThresholdSecondsFromSO;

    private void Awake()
    {
        levelIndex = SceneManager.GetActiveScene().buildIndex;

        if (!targetToRecord)
        {
            var tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged) targetToRecord = tagged.transform;
        }

        ReadMedals(out bestTimeAtLevelStart, out bronzeAlreadyAcquired, out bronzeThresholdSecondsFromSO);

        winObserver = new ActionObserver(OnLevelWon);
        WinTrigger.OnWinSaveTimes.Attach(winObserver);

        if (startRecordingOnEnable) StartRecording();
        Log($"Awake | best@start={bestTimeAtLevelStart:0.###} bronzeAcq={bronzeAlreadyAcquired} bronzeThr={bronzeThresholdSecondsFromSO:0.###}");
    }

    private void OnEnable()
    {
        if (startRecordingOnEnable && !isRecording) StartRecording();
    }

    private void OnDisable() { StopRecording(); }

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

    private void Update() { if (!recordAtFixedUpdate) TickRecord(Time.deltaTime); }
    private void FixedUpdate() { if (recordAtFixedUpdate) TickRecord(Time.fixedDeltaTime); }

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
        float finalTime = Game.LevelManager.LevelCompleteTime();

        bool isPB = IsNewRecord(finalTime, bestTimeAtLevelStart);
        bool bronzeByThreshold = bronzeThresholdSecondsFromSO > 0f && finalTime <= bronzeThresholdSecondsFromSO;
        bool bronzeGate = !requireBronzeToSave || bronzeAlreadyAcquired || bronzeByThreshold;

        Log($"Win t={finalTime:0.###} | PB={isPB} | bronzeGate={bronzeGate} (thr={bronzeThresholdSecondsFromSO:0.###})");

        EnsureFolderExists();

        if (alsoSaveLastRun)
        {
            SaveRunToPath(currentRun, GetLastFilePathForLevel(levelIndex));
            Log("Saved LAST");
        }

        if (bronzeGate && (isPB || !requireNewRecordToSave))
        {
            SaveRunToPath(currentRun, GetBestFilePathForLevel(levelIndex));
            Log("Saved BEST");
        }

        StopRecording();
    }

    private static bool IsNewRecord(float candidate, float bestAtStart)
    {
        if (bestAtStart <= 0f) return true;
        return candidate < bestAtStart - 0.0005f;
    }

    private void ReadMedals(out float bestTime, out bool bronzeAcquired, out float bronzeThreshold)
    {
        bestTime = 0f; bronzeAcquired = false; bronzeThreshold = 0f;

        var list = Game.LevelManager.GetMedals();
        if (list == null) return;

        LevelMedalsSO entry = null;
        for (int i = 0; i < list.Count; i++)
        {
            var m = list[i];
            if (m && m.levelNumber == levelIndex) { entry = m; break; }
        }
        if (!entry) return;

        var times = entry.levelMedalTimes;

        float best = float.MaxValue; bool found = false;

        if (times.bronze.isAcquired) { best = Mathf.Min(best, times.bronze.time); found = true; bronzeAcquired = true; }
        if (times.silver.isAcquired) { best = Mathf.Min(best, times.silver.time); found = true; }
        if (times.gold.isAcquired)   { best = Mathf.Min(best, times.gold.time);   found = true; }

        bestTime = found ? best : 0f;

        bronzeThreshold = times.bronze.time; // threshold to earn bronze
    }

    private string GetFolderPath() => Path.Combine(Application.persistentDataPath, ghostsFolderName);
    private string GetBestFilePathForLevel(int idx) => Path.Combine(GetFolderPath(), bestFileNamePattern.Replace("{LEVEL}", idx.ToString()));
    private string GetLastFilePathForLevel(int idx) => Path.Combine(GetFolderPath(), lastFileNamePattern.Replace("{LEVEL}", idx.ToString()));

    private void EnsureFolderExists()
    {
        string folder = GetFolderPath();
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
    }

    private void SaveRunToPath(GhostRunData run, string path)
    {
        if (run == null || run.frames == null || run.frames.Count < 2) return;
        File.WriteAllText(path, JsonUtility.ToJson(run));
    }

    public static bool TryLoadBestGhostForCurrentLevel(out GhostRunData data)
    {
        int level = SceneManager.GetActiveScene().buildIndex;
        string path = Path.Combine(Application.persistentDataPath, "ghosts", $"level_{level}.ghost.json");
        if (File.Exists(path))
        {
            data = JsonUtility.FromJson<GhostRunData>(File.ReadAllText(path));
            return data != null && data.levelIndex == level;
        }
        data = null;
        return false;
    }

    public static bool TryLoadLastGhostForCurrentLevel(out GhostRunData data)
    {
        int level = SceneManager.GetActiveScene().buildIndex;
        string path = Path.Combine(Application.persistentDataPath, "ghosts", $"last_level_{level}.ghost.json");
        if (File.Exists(path))
        {
            data = JsonUtility.FromJson<GhostRunData>(File.ReadAllText(path));
            return data != null && data.levelIndex == level;
        }
        data = null;
        return false;
    }

    [Obsolete("Use TryLoadBestGhostForCurrentLevel instead.")]
    public static bool TryLoadGhostForCurrentLevel(out GhostRunData data)
    {
        if (TryLoadBestGhostForCurrentLevel(out data)) return true;
        return TryLoadLastGhostForCurrentLevel(out data);
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