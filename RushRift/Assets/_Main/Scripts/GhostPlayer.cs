using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class GhostPlayer : MonoBehaviour
{
    public enum RotationMode { UseRecordedRotation, FaceVelocity, IgnoreRotation }

    [Header("Bronze Requirement")]
    [SerializeField, Tooltip("If enabled, the visual ghost prefab will only be spawned if the player's best time for this level meets the Bronze threshold.")]
    private bool requireBronzeToSpawn = true;

    [SerializeField, Tooltip("Bronze threshold in seconds for this level.")]
    private float bronzeThresholdSeconds = 60f;

    [Header("Ghost Source")]
    [SerializeField, Tooltip("Auto-load the saved ghost for the active level on enable.")]
    private bool autoLoadOnEnable = true;

    [SerializeField, Tooltip("Load the ghost recorded by GhostRecorder.TryLoadGhostForCurrentLevel().")]
    private bool loadSavedGhostForCurrentLevel = true;

    [Header("Ghost Visual")]
    [SerializeField, Tooltip("Prefab used as the ghost placeholder (e.g., a Particle System or mesh). If null, this GameObject will be moved.")]
    private GameObject ghostVisualPrefab;

    [SerializeField, Tooltip("Parent the spawned ghost under this object.")]
    private bool parentGhostUnderThis = true;

    [SerializeField, Tooltip("Offset applied to the recorded positions.")]
    private Vector3 worldPositionOffset = Vector3.zero;

    [Header("Playback")]
    [SerializeField, Tooltip("Start playback automatically on enable if a ghost is available.")]
    private bool beginPlaybackOnEnable = true;

    [SerializeField, Tooltip("Playback speed multiplier.")]
    private float playbackSpeed = 1f;

    [SerializeField, Tooltip("Loop playback when the end is reached.")]
    private bool loopPlayback = false;

    [SerializeField, Tooltip("Use unscaled time for playback.")]
    private bool useUnscaledTime = false;

    [SerializeField, Tooltip("How the ghost's rotation is handled.")]
    private RotationMode rotationMode = RotationMode.UseRecordedRotation;

    [SerializeField, Tooltip("Minimum distance per second required to face velocity in FaceVelocity mode.")]
    private float faceVelocityMinSpeed = 0.05f;

    [Header("Visibility")]
    [SerializeField, Tooltip("Initial visibility of the ghost visual.")]
    private bool initialGhostVisible = true;

    [SerializeField, Tooltip("Key to toggle the ghost visibility during play.")]
    private KeyCode toggleVisibilityKey = KeyCode.H;

    [Header("Pause Integration")]
    [SerializeField, Tooltip("If enabled, the ghost pauses/resumes with the game via PauseEventBus.")]
    private bool obeyPauseEvents = true;

    [Header("Controls")]
    [SerializeField, Tooltip("Hotkey to toggle play/pause (optional).")]
    private KeyCode togglePlayKey = KeyCode.None;

    [Header("Debug")]
    [SerializeField, Tooltip("If enabled, prints detailed logs.")]
    private bool isDebugLoggingEnabled = false;

    [SerializeField, Tooltip("Draw gizmos for the path.")]
    private bool drawGizmos = true;

    [SerializeField, Tooltip("Max segments to draw.")]
    private int gizmoMaxSegments = 256;

    [SerializeField, Tooltip("Gizmo color for the path.")]
    private Color gizmoPathColor = new Color(0f, 1f, 0.6f, 0.85f);

    private GhostRecorder.GhostRunData loadedRun;
    private Transform ghostTransform;
    private bool isPlaying;
    private float playbackTime;
    private int nextFrameIndex;
    private readonly List<Vector3> cachedPositions = new();
    private bool isGhostVisible;
    private readonly List<Renderer> cachedRenderers = new();
    private readonly List<ParticleSystem> cachedParticles = new();
    private bool wasPlayingBeforePause;
    private bool bronzeAchieved;

    private void OnEnable()
    {
        if (obeyPauseEvents) PauseEventBus.PauseChanged += OnPauseChanged;

        bronzeAchieved = !requireBronzeToSpawn || HasAchievedBronze();

        if (autoLoadOnEnable) LoadGhost();
        EnsureGhostVisual();
        SetGhostVisible(initialGhostVisible);

        if (beginPlaybackOnEnable && HasValidRun() && !PauseEventBus.IsPaused) Play();
    }

    private void OnDisable()
    {
        if (obeyPauseEvents) PauseEventBus.PauseChanged -= OnPauseChanged;
        Pause();
    }

    private void Update()
    {
        if (togglePlayKey != KeyCode.None && Input.GetKeyDown(togglePlayKey))
        {
            if (isPlaying) Pause(); else if (!PauseEventBus.IsPaused) Play();
        }

        if (toggleVisibilityKey != KeyCode.None && Input.GetKeyDown(toggleVisibilityKey))
            ToggleGhostVisible();

        if (PauseEventBus.IsPaused) return;
        if (!isPlaying || !HasValidRun() || ghostTransform == null) return;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float dur = GetDuration();
        if (dur <= 0f) return;

        playbackTime += Mathf.Max(0f, dt) * Mathf.Max(0.0001f, playbackSpeed);

        if (playbackTime >= dur)
        {
            if (loopPlayback)
            {
                playbackTime %= dur;
                nextFrameIndex = 1;
            }
            else
            {
                playbackTime = dur;
                ApplyPoseAtTime(dur);
                Pause();
                return;
            }
        }

        ApplyPoseAtTime(playbackTime);
    }

    private void OnPauseChanged(bool paused)
    {
        if (!obeyPauseEvents) return;

        if (paused)
        {
            wasPlayingBeforePause = isPlaying;
            if (isPlaying) Pause();
        }
        else
        {
            if (wasPlayingBeforePause && HasValidRun()) Play();
        }
    }

    public void LoadGhost()
    {
        GhostRecorder.GhostRunData data;
        if (loadSavedGhostForCurrentLevel && GhostRecorder.TryLoadGhostForCurrentLevel(out data))
        {
            loadedRun = data;
            CachePositions();
            Log($"Loaded ghost with {loadedRun.frames.Count} frames, duration {loadedRun.durationSeconds:0.###}s");
        }
        else
        {
            loadedRun = null;
            cachedPositions.Clear();
            Log("No ghost found for current level");
        }
        nextFrameIndex = 1;
        playbackTime = 0f;
        
        TrySpawnGhostVisualIfNeeded();
    }

    public void SetGhost(GhostRecorder.GhostRunData run)
    {
        loadedRun = run;
        CachePositions();
        nextFrameIndex = 1;
        playbackTime = 0f;
        
        TrySpawnGhostVisualIfNeeded();
    }

    public void Play()
    {
        if (!HasValidRun()) { Log("Play ignored: no run"); return; }
        EnsureGhostVisual();
        isPlaying = true;
        Log("Play");
    }

    public void Pause()
    {
        isPlaying = false;
        Log("Pause");
    }

    public void Stop()
    {
        isPlaying = false;
        playbackTime = 0f;
        nextFrameIndex = 1;
        if (HasValidRun()) ApplyPoseAtTime(0f);
        Log("Stop");
    }

    public void SetGhostVisible(bool visible)
    {
        isGhostVisible = visible;
        if (!ghostTransform) return;

        if (ghostTransform != transform)
        {
            ghostTransform.gameObject.SetActive(isGhostVisible);
            return;
        }

        if (cachedRenderers.Count == 0 && cachedParticles.Count == 0) CacheVisualComponents();

        foreach (var r in cachedRenderers) if (r) r.enabled = isGhostVisible;
        foreach (var ps in cachedParticles)
        {
            if (!ps) continue;
            if (isGhostVisible) ps.Play(true);
            else { ps.Pause(true); ps.Clear(true); }
        }
    }

    public void ToggleGhostVisible()
    {
        SetGhostVisible(!isGhostVisible);
        Log(isGhostVisible ? "Ghost visible" : "Ghost hidden");
    }
    
    private void EnsureGhostVisual()
    {
        if (ghostTransform != null) return;

        if (ghostVisualPrefab && bronzeAchieved && HasValidRun())
        {
            var go = Instantiate(ghostVisualPrefab, Vector3.zero, Quaternion.identity,
                parentGhostUnderThis ? transform : null);
            ghostTransform = go.transform;
            Log("Spawned ghost visual prefab");
        }
        else
        {
            ghostTransform = transform;
            Log(bronzeAchieved
                ? "No valid run available, NOT spawning prefab (using self transform)"
                : "Bronze not achieved, NOT spawning prefab (using self transform)");
        }

        CacheVisualComponents();
        if (HasValidRun()) ApplyPoseAtTime(0f);
    }


    private void CacheVisualComponents()
    {
        cachedRenderers.Clear();
        cachedParticles.Clear();
        if (!ghostTransform) return;

        if (ghostTransform == transform)
        {
            cachedRenderers.AddRange(GetComponentsInChildren<Renderer>(true));
            cachedParticles.AddRange(GetComponentsInChildren<ParticleSystem>(true));
        }
        else
        {
            cachedRenderers.AddRange(ghostTransform.GetComponentsInChildren<Renderer>(true));
            cachedParticles.AddRange(ghostTransform.GetComponentsInChildren<ParticleSystem>(true));
        }
    }

    private bool HasValidRun()
    {
        return loadedRun != null && loadedRun.frames != null && loadedRun.frames.Count >= 2 && loadedRun.durationSeconds > 0f;
    }
    
    private void TrySpawnGhostVisualIfNeeded()
    {
        bool canSpawnPrefab = ghostVisualPrefab && bronzeAchieved && HasValidRun();

        if (!canSpawnPrefab) return;
        if (ghostTransform != null && ghostTransform != transform) return; 
        var go = Instantiate(ghostVisualPrefab, Vector3.zero, Quaternion.identity,
            parentGhostUnderThis ? transform : null);
        ghostTransform = go.transform;
        CacheVisualComponents();
        if (HasValidRun()) ApplyPoseAtTime(0f);
        Log("Spawned ghost visual prefab (run available)");
    }


    private float GetDuration()
    {
        return loadedRun != null ? Mathf.Max(loadedRun.durationSeconds, loadedRun.frames[loadedRun.frames.Count - 1].time) : 0f;
    }

    private void ApplyPoseAtTime(float t)
    {
        var frames = loadedRun.frames;
        int count = frames.Count;

        while (nextFrameIndex < count && frames[nextFrameIndex].time < t)
            nextFrameIndex++;

        int i1 = Mathf.Clamp(nextFrameIndex, 1, count - 1);
        int i0 = i1 - 1;

        var f0 = frames[i0];
        var f1 = frames[i1];

        float span = Mathf.Max(1e-5f, f1.time - f0.time);
        float u = Mathf.Clamp01((t - f0.time) / span);

        Vector3 pos = Vector3.Lerp(f0.position, f1.position, u) + worldPositionOffset;

        Quaternion rot;
        if (rotationMode == RotationMode.UseRecordedRotation)
        {
            rot = Quaternion.Slerp(f0.rotation, f1.rotation, u);
        }
        else if (rotationMode == RotationMode.FaceVelocity)
        {
            Vector3 v = (f1.position - f0.position) / span;
            if (v.sqrMagnitude > faceVelocityMinSpeed * faceVelocityMinSpeed)
                rot = Quaternion.LookRotation(v.normalized, Vector3.up);
            else
                rot = Quaternion.Slerp(f0.rotation, f1.rotation, u);
        }
        else
        {
            rot = ghostTransform.rotation;
        }

        ghostTransform.SetPositionAndRotation(pos, rot);
    }

    private bool HasAchievedBronze()
    {
        var data = SaveAndLoad.Load();
        int level = SceneManager.GetActiveScene().buildIndex;
        if (data != null && data.BestTimes != null && data.BestTimes.TryGetValue(level, out var best))
            return best > 0f && best <= bronzeThresholdSeconds;
        return false;
    }

    private void CachePositions()
    {
        cachedPositions.Clear();
        if (!HasValidRun()) return;
        var frames = loadedRun.frames;
        for (int i = 0; i < frames.Count; i++)
            cachedPositions.Add(frames[i].position + worldPositionOffset);
    }

    private void Log(string msg)
    {
        if (!isDebugLoggingEnabled) return;
        Debug.Log($"[GhostPlayer] {name}: {msg}", this);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        if (cachedPositions.Count < 2)
        {
            if (HasValidRun() && cachedPositions.Count == 0) CachePositions();
            if (cachedPositions.Count < 2) return;
        }

        Gizmos.color = gizmoPathColor;
        int count = cachedPositions.Count;
        int start = Mathf.Max(0, count - Mathf.Max(2, gizmoMaxSegments));
        for (int i = start + 1; i < count; i++)
        {
            Gizmos.DrawLine(cachedPositions[i - 1], cachedPositions[i]);
        }
    }
#endif
}
