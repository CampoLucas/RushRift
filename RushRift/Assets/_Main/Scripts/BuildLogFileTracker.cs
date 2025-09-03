using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;
using System.Globalization;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Profiling;

public sealed class BuildLogFileTracker : MonoBehaviour
{
    [Header("Activation")]
    [Tooltip("Start tracking logs automatically on Awake.")]
    [SerializeField] private bool activateOnAwake = true;

    [Tooltip("Enable internal debug messages from this tracker.")]
    [SerializeField] private bool enableInternalDebugLogs = false;

    [Header("Session")]
    [Tooltip("Automatically generate a new session GUID at start.")]
    [SerializeField] private bool autoGenerateSessionGuid = true;

    [Tooltip("Optional custom session GUID (overrides auto-generation when not empty).")]
    [SerializeField] private string customSessionGuid = "";

    [Header("File Output")]
    [Tooltip("Base file name without extension.")]
    [SerializeField] private string logFileBaseName = "PlayerLog";

    [Tooltip("File extension without dot.")]
    [SerializeField] private string logFileExtension = "txt";

    [Tooltip("Append the current date and time to the file name.")]
    [SerializeField] private bool appendDateTimeToFileName = true;

    [Tooltip("Include the session GUID in the file name.")]
    [SerializeField] private bool includeSessionGuidInFileName = true;

    [Tooltip("Create the log file in the build folder when possible. On unsupported platforms, persistentDataPath is used.")]
    [SerializeField] private bool preferBuildFolderOutput = true;

    [Header("Writing Behavior")]
    [Tooltip("Flush queued log lines every frame.")]
    [SerializeField] private bool flushEveryFrame = true;

    [Tooltip("Flush interval in seconds when not flushing every frame.")]
    [SerializeField, Min(0.05f)] private float flushIntervalSeconds = 1f;

    [Tooltip("Include stack trace text for errors, exceptions, and asserts.")]
    [SerializeField] private bool includeStackTraceForErrors = true;

    [Tooltip("Include a session header when the file is opened.")]
    [SerializeField] private bool includeSessionHeader = true;

    [Header("Summary Output")]
    [Tooltip("Write a final summary block to the file on shutdown.")]
    [SerializeField] private bool writeFinalSummaryToFile = true;

    [Tooltip("Also print the final summary to the Unity console.")]
    [SerializeField] private bool printFinalSummaryToUnityConsole = true;

    [Header("Performance")]
    [Tooltip("Calculate average FPS and include it in the final summary.")]
    [SerializeField] private bool calculateAverageFramesPerSecond = true;

    [Tooltip("Detect large frame spikes and log a snapshot.")]
    [SerializeField] private bool enablePerformanceSpikeDetection = true;

    [Tooltip("Frame time threshold in milliseconds to count as a performance spike.")]
    [SerializeField, Min(1f)] private float spikeFrameTimeMillisecondsThreshold = 50f;

    [Tooltip("Record and report the latest screen resolution.")]
    [SerializeField] private bool recordScreenResolution = true;

    [Header("Memory & GC")]
    [Tooltip("Periodically log memory statistics.")]
    [SerializeField] private bool enablePeriodicMemoryStats = true;

    [Tooltip("Interval in seconds for periodic memory stats.")]
    [SerializeField, Min(0.25f)] private float memoryStatsIntervalSeconds = 5f;

    [Tooltip("Track GC collections and accumulated managed allocation growth across the session.")]
    [SerializeField] private bool enableGarbageCollectionMonitoring = true;

    [Header("Lifecycle & Scenes")]
    [Tooltip("Log focus and pause events with timestamps.")]
    [SerializeField] private bool logFocusAndPauseEvents = true;

    [Tooltip("Track scene load/unload/active changes with timestamps and durations.")]
    [SerializeField] private bool trackSceneLifecycleEvents = true;

    [Header("Gizmos")]
    [Tooltip("Draw gizmos to indicate this tracker exists in the scene.")]
    [SerializeField] private bool drawGizmos = true;

    [Tooltip("Gizmo color.")]
    [SerializeField] private Color gizmoColor = new Color(0.2f, 1f, 0.4f, 0.6f);

    private static BuildLogFileTracker sharedInstance;
    private readonly ConcurrentQueue<string> pendingLogLineQueue = new ConcurrentQueue<string>();
    private FileStream fileStream;
    private StreamWriter streamWriter;
    private string resolvedOutputDirectoryPath;
    private string resolvedOutputFilePath;

    private string sessionGuid;
    private float timeSinceLastFlush;
    private bool isTracking;
    private bool finalSummaryWritten;

    private long totalErrorCount;
    private long totalWarningCount;
    private long totalAssertCount;
    private long totalLogCount;

    private DateTime sessionStartTimeUtc;
    private long totalFrameCountForAverageFps;
    private double accumulatedUnscaledDeltaTimeSeconds;
    private string latestScreenResolutionString;

    private float memoryStatsTimer;
    private long lastMonoUsedBytes;
    private long accumulatedManagedAllocationDeltaBytes;
    private int lastGcGen0;
    private int lastGcGen1;
    private int lastGcGen2;

    private DateTime activeSceneStartTimeUtc;
    private string activeSceneName;
    private readonly Dictionary<int, DateTime> sceneLoadStartTimesUtcByBuildIndex = new Dictionary<int, DateTime>();

    private void Awake()
    {
        if (sharedInstance != null && sharedInstance != this)
        {
            if (enableInternalDebugLogs) Debug.Log("[BuildLogFileTracker] Destroying duplicate instance.");
            Destroy(gameObject);
            return;
        }

        sharedInstance = this;
        DontDestroyOnLoad(gameObject);

        if (activateOnAwake) StartTrackingLogsToFile();
    }

    private void OnEnable()
    {
        if (isTracking) SubscribeCallbacks();
    }

    private void OnDisable()
    {
        UnsubscribeCallbacks();
        FlushQueuedLogsToFile();
    }

    private void OnDestroy()
    {
        StopTrackingLogsToFile();
        if (sharedInstance == this) sharedInstance = null;
    }

    private void Update()
    {
        if (!isTracking) return;

        if (calculateAverageFramesPerSecond)
        {
            totalFrameCountForAverageFps++;
            accumulatedUnscaledDeltaTimeSeconds += Time.unscaledDeltaTime;
        }

        if (enablePerformanceSpikeDetection)
        {
            float frameMs = Time.unscaledDeltaTime * 1000f;
            if (frameMs >= spikeFrameTimeMillisecondsThreshold)
            {
                string sceneInfo = GetActiveSceneInfoString();
                string memInfo = BuildMemorySnapshotString();
                AddEventLineToQueue("SPIKE", "FrameTimeMs=" + frameMs.ToString("0.0") + "  " + sceneInfo + "  " + memInfo);
            }
        }

        if (recordScreenResolution)
        {
            string current = ComputeCurrentResolutionString();
            if (!string.Equals(current, latestScreenResolutionString)) latestScreenResolutionString = current;
        }

        if (enablePeriodicMemoryStats)
        {
            memoryStatsTimer += Time.unscaledDeltaTime;
            if (memoryStatsTimer >= memoryStatsIntervalSeconds)
            {
                memoryStatsTimer = 0f;
                AddEventLineToQueue("MEMORY", BuildMemorySnapshotString());
            }
        }

        if (enableGarbageCollectionMonitoring)
        {
            MonitorGarbageCollectionsAndAllocations();
        }

        if (flushEveryFrame)
        {
            FlushQueuedLogsToFile();
        }
        else
        {
            timeSinceLastFlush += Time.unscaledDeltaTime;
            if (timeSinceLastFlush >= flushIntervalSeconds)
            {
                FlushQueuedLogsToFile();
                timeSinceLastFlush = 0f;
            }
        }
    }

    public void StartTrackingLogsToFile()
    {
        if (isTracking) return;

        ResetSessionState();

        sessionGuid = ResolveSessionGuid();
        latestScreenResolutionString = ComputeCurrentResolutionString();

        ResolveOutputPaths();
        OpenFileStream();

        if (includeSessionHeader)
        {
            AddEventLineToQueue("SESSION", "Started " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  " + SystemInfo.operatingSystem);
            AddEventLineToQueue("SESSION", "SessionGUID=" + sessionGuid);
            AddEventLineToQueue("SESSION", "Application=" + Application.productName + " " + Application.version + "  BuildGUID=" + Application.buildGUID + "  Identifier=" + Application.identifier);
            AddEventLineToQueue("SESSION", "Platform=" + Application.platform + "  Unity=" + Application.unityVersion);
            AddEventLineToQueue("SESSION", "Resolution=" + latestScreenResolutionString);
            AddEventLineToQueue("SESSION", "Path=" + resolvedOutputFilePath);
            foreach (var line in BuildDeviceSnapshotLines()) EnqueueFormattedLogLine(line);
            EnqueueFormattedLogLine("=======================================================================");
        }

        SubscribeCallbacks();
        isTracking = true;

        if (enableInternalDebugLogs) Debug.Log("[BuildLogFileTracker] Tracking started. Writing to: " + resolvedOutputFilePath);
    }

    public void StopTrackingLogsToFile()
    {
        if (!isTracking && finalSummaryWritten) return;

        UnsubscribeCallbacks();
        FlushQueuedLogsToFile();

        if (writeFinalSummaryToFile && !finalSummaryWritten)
        {
            WriteFinalSummary();
            finalSummaryWritten = true;
        }

        FlushQueuedLogsToFile();
        CloseFileStream();
        isTracking = false;

        if (enableInternalDebugLogs) Debug.Log("[BuildLogFileTracker] Tracking stopped.");
    }

    private void SubscribeCallbacks()
    {
        Application.logMessageReceivedThreaded += HandleLogMessageReceivedThreaded;
        Application.quitting += HandleApplicationQuitting;
        if (logFocusAndPauseEvents)
        {
            Application.focusChanged += HandleApplicationFocusChanged;
        }
        if (trackSceneLifecycleEvents)
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            SceneManager.sceneUnloaded += HandleSceneUnloaded;
            SceneManager.activeSceneChanged += HandleActiveSceneChanged;
            var s = SceneManager.GetActiveScene();
            activeSceneName = s.name;
            activeSceneStartTimeUtc = DateTime.UtcNow;
        }
    }

    private void UnsubscribeCallbacks()
    {
        Application.logMessageReceivedThreaded -= HandleLogMessageReceivedThreaded;
        Application.quitting -= HandleApplicationQuitting;
        Application.focusChanged -= HandleApplicationFocusChanged;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneUnloaded -= HandleSceneUnloaded;
        SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
    }

    private void HandleApplicationQuitting()
    {
        if (enableInternalDebugLogs) Debug.Log("[BuildLogFileTracker] Application quitting. Flushing log file.");
        StopTrackingLogsToFile();
    }

    private void HandleApplicationFocusChanged(bool hasFocus)
    {
        if (!logFocusAndPauseEvents) return;
        AddEventLineToQueue("FOCUS", "HasFocus=" + hasFocus);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!trackSceneLifecycleEvents) return;
        sceneLoadStartTimesUtcByBuildIndex[scene.buildIndex] = DateTime.UtcNow;
        AddEventLineToQueue("SCENE_LOADED", "Name=" + scene.name + "  Index=" + scene.buildIndex + "  Path=" + scene.path + "  Mode=" + mode);
    }

    private void HandleSceneUnloaded(Scene scene)
    {
        if (!trackSceneLifecycleEvents) return;
        DateTime started;
        TimeSpan dur = TimeSpan.Zero;
        if (sceneLoadStartTimesUtcByBuildIndex.TryGetValue(scene.buildIndex, out started))
        {
            dur = DateTime.UtcNow - started;
            sceneLoadStartTimesUtcByBuildIndex.Remove(scene.buildIndex);
        }
        AddEventLineToQueue("SCENE_UNLOADED", "Name=" + scene.name + "  Index=" + scene.buildIndex + "  Path=" + scene.path + "  Duration=" + dur);
    }

    private void HandleActiveSceneChanged(Scene prev, Scene next)
    {
        if (!trackSceneLifecycleEvents) return;
        if (!string.IsNullOrEmpty(activeSceneName))
        {
            var dur = DateTime.UtcNow - activeSceneStartTimeUtc;
            AddEventLineToQueue("SCENE_ACTIVE_DURATION", "Name=" + activeSceneName + "  Duration=" + dur);
        }
        activeSceneName = next.name;
        activeSceneStartTimeUtc = DateTime.UtcNow;
        AddEventLineToQueue("SCENE_ACTIVE_CHANGED", "From=" + prev.name + "  To=" + next.name + "  Index=" + next.buildIndex + "  Path=" + next.path);
    }

    private void HandleLogMessageReceivedThreaded(string logString, string stackTrace, LogType logType)
    {
        switch (logType)
        {
            case LogType.Log: Interlocked.Increment(ref totalLogCount); break;
            case LogType.Warning: Interlocked.Increment(ref totalWarningCount); break;
            case LogType.Assert: Interlocked.Increment(ref totalAssertCount); break;
            case LogType.Error:
            case LogType.Exception: Interlocked.Increment(ref totalErrorCount); break;
        }

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string typeText = logType.ToString().ToUpperInvariant();
        StringBuilder builder = new StringBuilder(256);
        builder.Append('[').Append(timestamp).Append("] [").Append(typeText).Append("] ").Append(logString);

        if (includeStackTraceForErrors)
        {
            if (logType == LogType.Error || logType == LogType.Exception || logType == LogType.Assert)
            {
                if (!string.IsNullOrEmpty(stackTrace))
                {
                    builder.AppendLine();
                    builder.Append(stackTrace);
                }
            }
        }

        pendingLogLineQueue.Enqueue(builder.ToString());
    }

    private void EnqueueFormattedLogLine(string line)
    {
        pendingLogLineQueue.Enqueue(line);
    }

    private void AddEventLineToQueue(string category, string text)
    {
        string line = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "] [" + category + "] " + text;
        pendingLogLineQueue.Enqueue(line);
        if (enableInternalDebugLogs) Debug.Log("[BuildLogFileTracker][" + category + "] " + text);
    }

    private void FlushQueuedLogsToFile()
    {
        if (streamWriter == null) return;
        while (pendingLogLineQueue.TryDequeue(out string line))
        {
            streamWriter.WriteLine(line);
        }
        streamWriter.Flush();
    }

    private void ResolveOutputPaths()
    {
        string directoryPath = ResolveBuildFolderPathOrFallback();
        if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);

        string fileCore = logFileBaseName;
        if (includeSessionGuidInFileName && !string.IsNullOrEmpty(sessionGuid)) fileCore = fileCore + "_" + sessionGuid;
        if (appendDateTimeToFileName) fileCore = fileCore + "_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

        string fileName = fileCore + "." + logFileExtension;

        resolvedOutputDirectoryPath = directoryPath;
        resolvedOutputFilePath = Path.Combine(directoryPath, fileName);
    }

    private string ResolveBuildFolderPathOrFallback()
    {
        if (!preferBuildFolderOutput) return Application.persistentDataPath;

        string candidate = null;

        #if UNITY_STANDALONE_OSX
        try
        {
            DirectoryInfo contents = Directory.GetParent(Application.dataPath);
            DirectoryInfo appBundle = contents != null ? contents.Parent : null;
            DirectoryInfo buildFolder = appBundle != null ? appBundle.Parent : null;
            candidate = buildFolder != null ? buildFolder.FullName : null;
        }
        catch { candidate = null; }
        #else
        try
        {
            DirectoryInfo dataDir = Directory.GetParent(Application.dataPath);
            candidate = dataDir != null ? dataDir.FullName : null;
        }
        catch { candidate = null; }
        #endif

        if (string.IsNullOrEmpty(candidate)) candidate = Application.persistentDataPath;
        return candidate;
    }

    private void OpenFileStream()
    {
        CloseFileStream();
        fileStream = new FileStream(resolvedOutputFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        streamWriter = new StreamWriter(fileStream, new UTF8Encoding(false));
    }

    private void CloseFileStream()
    {
        try { streamWriter?.Flush(); } catch {}
        try { streamWriter?.Dispose(); } catch {}
        try { fileStream?.Dispose(); } catch {}
        streamWriter = null;
        fileStream = null;
    }

    private void WriteFinalSummary()
    {
        TimeSpan duration = DateTime.UtcNow - sessionStartTimeUtc;
        double avgFps = calculateAverageFramesPerSecond && accumulatedUnscaledDeltaTimeSeconds > 0.0
            ? Math.Max(0.0, totalFrameCountForAverageFps / Math.Max(0.000001, accumulatedUnscaledDeltaTimeSeconds))
            : 0.0;

        string summary =
            "Summary: " +
            "Errors=" + totalErrorCount +
            "  Warnings=" + totalWarningCount +
            "  Asserts=" + totalAssertCount +
            "  Logs=" + totalLogCount +
            "  Duration=" + duration +
            "  AverageFPS=" + (avgFps > 0.0 ? avgFps.ToString("0.00") : "n/a") +
            "  Frames=" + totalFrameCountForAverageFps +
            "  Resolution=" + (recordScreenResolution ? latestScreenResolutionString : "n/a") +
            "  ManagedAllocDeltaMB=" + (accumulatedManagedAllocationDeltaBytes > 0 ? (accumulatedManagedAllocationDeltaBytes / (1024f * 1024f)).ToString("0.0") : "0.0");

        EnqueueFormattedLogLine("===== Log Session Ended " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " =====");
        EnqueueFormattedLogLine("SessionGUID=" + sessionGuid);
        EnqueueFormattedLogLine(summary);
        EnqueueFormattedLogLine("=======================================================================");

        if (printFinalSummaryToUnityConsole) Debug.Log("[BuildLogFileTracker] " + summary);
    }

    private void ResetSessionState()
    {
        Interlocked.Exchange(ref totalErrorCount, 0);
        Interlocked.Exchange(ref totalWarningCount, 0);
        Interlocked.Exchange(ref totalAssertCount, 0);
        Interlocked.Exchange(ref totalLogCount, 0);
        totalFrameCountForAverageFps = 0;
        accumulatedUnscaledDeltaTimeSeconds = 0.0;
        finalSummaryWritten = false;
        memoryStatsTimer = 0f;
        lastMonoUsedBytes = Profiler.GetMonoUsedSizeLong();
        accumulatedManagedAllocationDeltaBytes = 0;
        lastGcGen0 = GC.CollectionCount(0);
        lastGcGen1 = GC.CollectionCount(1);
        lastGcGen2 = GC.CollectionCount(2);
        sessionStartTimeUtc = DateTime.UtcNow;
        var s = SceneManager.GetActiveScene();
        activeSceneName = s.name;
        activeSceneStartTimeUtc = DateTime.UtcNow;
        sceneLoadStartTimesUtcByBuildIndex.Clear();
    }

    private string ResolveSessionGuid()
    {
        if (!string.IsNullOrEmpty(customSessionGuid)) return customSessionGuid.Trim();
        if (autoGenerateSessionGuid) return Guid.NewGuid().ToString("N");
        return Guid.NewGuid().ToString("N");
    }

    private string ComputeCurrentResolutionString()
    {
        int w = Screen.width;
        int h = Screen.height;
        int rr = Screen.currentResolution.refreshRate;
        float dpi = Screen.dpi;
        string baseStr = w + "x" + h + (rr > 0 ? "@" + rr + "Hz" : "");
        if (dpi > 0f) baseStr += " " + dpi.ToString("0") + "DPI";
        return baseStr;
    }

    private string BuildMemorySnapshotString()
    {
        long alloc = Profiler.GetTotalAllocatedMemoryLong();
        long reserv = Profiler.GetTotalReservedMemoryLong();
        long unres = Profiler.GetTotalUnusedReservedMemoryLong();
        long monoHeap = Profiler.GetMonoHeapSizeLong();
        long monoUsed = Profiler.GetMonoUsedSizeLong();
        return "AllocatedMB=" + ToMB(alloc) + "  ReservedMB=" + ToMB(reserv) + "  UnusedReservedMB=" + ToMB(unres) + "  MonoHeapMB=" + ToMB(monoHeap) + "  MonoUsedMB=" + ToMB(monoUsed);
    }

    private string GetActiveSceneInfoString()
    {
        var s = SceneManager.GetActiveScene();
        var dur = DateTime.UtcNow - activeSceneStartTimeUtc;
        return "Scene=" + s.name + "  Index=" + s.buildIndex + "  Duration=" + dur;
    }

    private string ToMB(long bytes)
    {
        return (bytes / (1024f * 1024f)).ToString("0.0");
    }

    private void MonitorGarbageCollectionsAndAllocations()
    {
        int g0 = GC.CollectionCount(0);
        int g1 = GC.CollectionCount(1);
        int g2 = GC.CollectionCount(2);

        if (g0 != lastGcGen0 || g1 != lastGcGen1 || g2 != lastGcGen2)
        {
            int d0 = g0 - lastGcGen0;
            int d1 = g1 - lastGcGen1;
            int d2 = g2 - lastGcGen2;
            lastGcGen0 = g0;
            lastGcGen1 = g1;
            lastGcGen2 = g2;

            long monoUsed = Profiler.GetMonoUsedSizeLong();
            long delta = monoUsed - lastMonoUsedBytes;
            if (delta > 0) accumulatedManagedAllocationDeltaBytes += delta;
            lastMonoUsedBytes = monoUsed;

            AddEventLineToQueue("GC", "Gen0+=" + Math.Max(0, d0) + "  Gen1+=" + Math.Max(0, d1) + "  Gen2+=" + Math.Max(0, d2) + "  MonoUsedMB=" + ToMB(monoUsed) + "  AllocDeltaSessionMB=" + ToMB(accumulatedManagedAllocationDeltaBytes));
        }
    }

    private IEnumerable<string> BuildDeviceSnapshotLines()
    {
        string tz = TimeZoneInfo.Local.DisplayName;
        string tzOffset = TimeZoneInfo.Local.BaseUtcOffset.ToString();
        string culture = CultureInfo.CurrentCulture.Name;
        string cmd = string.Join(" ", Environment.GetCommandLineArgs().Select(a => a.Contains(" ") ? "\"" + a + "\"" : a).ToArray());

        yield return "[SNAPSHOT] DeviceModel=" + SystemInfo.deviceModel;
        yield return "[SNAPSHOT] CPU=" + SystemInfo.processorType + "  Cores=" + SystemInfo.processorCount + "  RAM_MB=" + SystemInfo.systemMemorySize;
        yield return "[SNAPSHOT] GPU=" + SystemInfo.graphicsDeviceName + "  API=" + SystemInfo.graphicsDeviceType + "  Version=" + SystemInfo.graphicsDeviceVersion + "  VRAM_MB=" + SystemInfo.graphicsMemorySize;
        yield return "[SNAPSHOT] RefreshRate=" + (Screen.currentResolution.refreshRate > 0 ? Screen.currentResolution.refreshRate + "Hz" : "n/a");
        yield return "[SNAPSHOT] Locale=" + culture + "  TimeZone=" + tz + " (" + tzOffset + ")";
        yield return "[SNAPSHOT] CommandLine=" + cmd;
        yield return "[SNAPSHOT] AppId=" + Application.identifier + "  BuildGUID=" + Application.buildGUID + "  Version=" + Application.version;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        Gizmos.color = gizmoColor;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one * 0.25f);
    }

    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        string label1 = isTracking ? "BuildLogFileTracker (Active)" : "BuildLogFileTracker (Idle)";
        string label2 = "Session=" + (string.IsNullOrEmpty(sessionGuid) ? "-" : sessionGuid);
        string label3 = "E=" + totalErrorCount + "  W=" + totalWarningCount + "  A=" + totalAssertCount + "  L=" + totalLogCount;
        string label4 = "FPS(avg)=" + (accumulatedUnscaledDeltaTimeSeconds > 0.0 ? (totalFrameCountForAverageFps / accumulatedUnscaledDeltaTimeSeconds).ToString("0.0") : "n/a") + "  Res=" + (string.IsNullOrEmpty(latestScreenResolutionString) ? "n/a" : latestScreenResolutionString);
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.60f, label1);
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.45f, label2);
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.30f, label3);
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.15f, label4);
        if (!string.IsNullOrEmpty(resolvedOutputFilePath))
            UnityEditor.Handles.Label(transform.position, resolvedOutputFilePath);
    }
    #endif
}