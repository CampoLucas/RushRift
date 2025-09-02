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
    private int maxRecordedFrames;

    [Header("Save Policy")]
    [SerializeField, Tooltip("Save the ghost only when a new personal best is set.")]
    private bool requireNewRecordToSave = true;
    [SerializeField, Tooltip("Require that the run beats this Bronze threshold to save. Disable to ignore Bronze.")]
    private bool requireBronzeToSave = true;
    [SerializeField, Tooltip("Bronze time threshold for this level in seconds.")]
    private float bronzeThresholdSeconds = 60f;

    [Header("Storage")]
    [SerializeField, Tooltip("Directory under persistentDataPath where ghosts are saved.")]
    private string ghostsFolderName = "ghosts";
    [SerializeField, Tooltip("Filename pattern per level. {LEVEL} is replaced by the buildIndex.")]
    private string fileNamePattern = "level_{LEVEL}.ghost.json";

    [Header("Debug")]
    [SerializeField, Tooltip("If enabled, prints detailed logs.")]
    private bool isDebugLoggingEnabled;
    [SerializeField, Tooltip("Draw gizmos for a portion of the recorded path.")]
    private bool drawGizmos = true;
    [SerializeField, Tooltip("How many recent segments to draw with gizmos.")]
    private int gizmoSegments = 64;
    [SerializeField, Tooltip("Gizmo color for the path.")]
    private Color gizmoColor = new Color(0f, 1f, 0.6f, 0.9f);

    [Serializable]
    public struct GhostFrame
    {
        public float time;
        public Vector3 position;
        public Quaternion rotation;
    }

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
    private float bestTimeAtLevelStart;

    private void Awake()
    {
        if (!targetToRecord)
        {
            var tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged) targetToRecord = tagged.transform;
        }

        bestTimeAtLevelStart = TryGetBestTime(SceneManager.GetActiveScene().buildIndex);

        winObserver = new ActionObserver(OnLevelWon);
        WinTrigger.OnWinSaveTimes.Attach(winObserver);

        if (startRecordingOnEnable) StartRecording();
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
            levelIndex = SceneManager.GetActiveScene().buildIndex,
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
        GhostFrame f;
        f.time = time;
        f.position = pos;
        f.rotation = rot;
        currentRun.frames.Add(f);

        lastFrameTime = time;
        lastPos = pos;
        lastRot = rot;

        if (maxRecordedFrames > 0 && currentRun.frames.Count > maxRecordedFrames)
            currentRun.frames.RemoveAt(0);
    }

    private void OnLevelWon()
    {
        float finalTime = Game.LevelManager.LevelCompleteTime();  // uses existing API :contentReference[oaicite:0]{index=0}
        bool newRecord = IsNewRecord(finalTime);
        bool bronzeOk = !requireBronzeToSave || finalTime <= bronzeThresholdSeconds;

        Log($"LevelWon t={finalTime:0.###}s newRecord={newRecord} bronzeOk={bronzeOk}");

        if ((!requireNewRecordToSave || newRecord) && bronzeOk)
        {
            EnsureFolderExists();
            SaveCurrentRunToFile();
        }

        StopRecording();
    }

    private bool IsNewRecord(float finalTime)
    {
        float prev = bestTimeAtLevelStart;
        if (prev <= 0f) return true;
        return finalTime <= prev + 0.0005f;
    }

    private float TryGetBestTime(int levelIndex)
    {
        var data = SaveAndLoad.Load();  // reads from persistent save :contentReference[oaicite:1]{index=1}
        if (data != null && data.BestTimes != null && data.BestTimes.ContainsKey(levelIndex))
            return data.BestTimes[levelIndex];
        return 0f;
    }

    private string GetFolderPath()
    {
        return Path.Combine(Application.persistentDataPath, ghostsFolderName);
    }

    private string GetFilePathForLevel(int levelIndex)
    {
        string file = fileNamePattern.Replace("{LEVEL}", levelIndex.ToString());
        return Path.Combine(GetFolderPath(), file);
    }

    private void EnsureFolderExists()
    {
        string folder = GetFolderPath();
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
    }

    private void SaveCurrentRunToFile()
    {
        if (currentRun == null || currentRun.frames == null || currentRun.frames.Count < 2) return;
        string path = GetFilePathForLevel(currentRun.levelIndex);
        string json = JsonUtility.ToJson(currentRun);
        File.WriteAllText(path, json);
        Log($"Saved ghost: {path}");
    }

    public static bool TryLoadGhostForCurrentLevel(out GhostRunData data)
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
        {
            var a = currentRun.frames[i - 1].position;
            var b = currentRun.frames[i].position;
            Gizmos.DrawLine(a, b);
        }
    }
#endif
}