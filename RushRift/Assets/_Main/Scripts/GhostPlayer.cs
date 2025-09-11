using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class GhostPlayer : MonoBehaviour
{
    public enum RotationMode { UseRecordedRotation, FaceVelocity, IgnoreRotation }
    public enum PositionInterpolationMode { Linear, CatmullRom }

    [Header("Ghost Source")]
    [SerializeField, Tooltip("Auto-load the best ghost for the active level on enable.")]
    private bool autoLoadOnEnable = true;

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

    [Header("Interpolation")]
    [SerializeField, Tooltip("How positions are interpolated between recorded frames.")]
    private PositionInterpolationMode positionInterpolation = PositionInterpolationMode.CatmullRom;
    [SerializeField, Tooltip("How the ghost's rotation is handled.")]
    private RotationMode rotationMode = RotationMode.UseRecordedRotation;
    [SerializeField, Tooltip("Minimum distance per second required to face velocity in FaceVelocity mode.")]
    private float faceVelocityMinSpeed = 0.05f;

    [Header("Output Smoothing")]
    [SerializeField, Tooltip("Apply extra smoothing to the final output pose.")]
    private bool applyOutputSmoothing = true;
    [SerializeField, Tooltip("Seconds to smooth positions to reduce jitter.")]
    private float positionSmoothTimeSeconds = 0.06f;
    [SerializeField, Tooltip("Seconds to smooth rotations to reduce jitter.")]
    private float rotationSmoothTimeSeconds = 0.06f;

    [Header("Visibility")]
    [SerializeField, Tooltip("Initial visibility of the ghost visual.")]
    private bool initialGhostVisible = true;
    [SerializeField, Tooltip("Key to toggle the ghost visibility during play.")]
    private KeyCode toggleVisibilityKey = KeyCode.G;

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
    [SerializeField, Tooltip("Path of the ghost file that was loaded (debug/info).")]
    private string debugLoadedGhostPath = "";

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

    private Vector3 smoothedPos;
    private Vector3 smoothedPosVel;
    private Quaternion smoothedRot = Quaternion.identity;

    private void OnEnable()
    {
        if (obeyPauseEvents) PauseEventBus.PauseChanged += OnPauseChanged;

        if (autoLoadOnEnable) LoadBestGhost();
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
        float dur = GetDuration(); if (dur <= 0f) return;

        playbackTime += Mathf.Max(0f, dt) * Mathf.Max(0.0001f, playbackSpeed);

        if (playbackTime >= dur)
        {
            if (loopPlayback) { playbackTime %= dur; nextFrameIndex = 1; }
            else { playbackTime = dur; ApplyPoseAtTime(dur, dt); Pause(); return; }
        }

        ApplyPoseAtTime(playbackTime, dt);
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

    public void LoadBestGhost()
    {
        GhostRecorder.GhostRunData data;
        string path;
        if (GhostRecorder.TryLoadBestGhostForCurrentLevel(out data, out path))
        {
            loadedRun = data;
            debugLoadedGhostPath = path;
            CachePositions();
            TrySpawnGhostVisualIfNeeded();

            var f0 = loadedRun.frames[0];
            smoothedPos = f0.position + worldPositionOffset;
            smoothedRot = f0.rotation;

            Log($"Loaded BEST ghost ({loadedRun.durationSeconds:0.###}s) from: {debugLoadedGhostPath}");
        }
        else
        {
            loadedRun = null;
            cachedPositions.Clear();
            debugLoadedGhostPath = "";
            Log($"No BEST ghost found for level {SceneManager.GetActiveScene().buildIndex}");
        }

        nextFrameIndex = 1;
        playbackTime = 0f;
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
        if (HasValidRun()) ApplyPoseAtTime(0f, 0f);
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

        if (ghostVisualPrefab && HasValidRun())
        {
            var go = Instantiate(ghostVisualPrefab, Vector3.zero, Quaternion.identity,
                                 parentGhostUnderThis ? transform : null);
            ghostTransform = go.transform;
            Log("Spawned ghost visual prefab");
        }
        else
        {
            ghostTransform = transform;
            Log("No valid run available, NOT spawning prefab (using self transform)");
        }

        CacheVisualComponents();
        if (HasValidRun()) ApplyPoseAtTime(0f, 0f);
    }

    private void TrySpawnGhostVisualIfNeeded()
    {
        bool canSpawnPrefab = ghostVisualPrefab && HasValidRun();
        if (!canSpawnPrefab) return;
        if (ghostTransform != null && ghostTransform != transform) return;

        var go = Instantiate(ghostVisualPrefab, Vector3.zero, Quaternion.identity,
                             parentGhostUnderThis ? transform : null);
        ghostTransform = go.transform;
        CacheVisualComponents();
        if (HasValidRun()) ApplyPoseAtTime(0f, 0f);
        Log("Spawned ghost visual prefab (run available)");
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

    private float GetDuration()
    {
        return loadedRun != null ? Mathf.Max(loadedRun.durationSeconds, loadedRun.frames[loadedRun.frames.Count - 1].time) : 0f;
    }

    private void ApplyPoseAtTime(float t, float dt)
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

        Vector3 rawPos;
        if (positionInterpolation == PositionInterpolationMode.CatmullRom && count >= 4)
        {
            int im1 = Mathf.Max(0, i0 - 1);
            int i2  = Mathf.Min(count - 1, i1 + 1);

            var fm1 = frames[im1];
            var f2  = frames[i2];

            float t_1 = fm1.time;
            float t0  = f0.time;
            float t1  = f1.time;
            float t2  = f2.time;

            Vector3 p_1 = fm1.position;
            Vector3 p0  = f0.position;
            Vector3 p1  = f1.position;
            Vector3 p2  = f2.position;

            float m0Scale = (t1 - t_1) > 1e-5f ? 1f / (t1 - t_1) : 0f;
            float m1Scale = (t2 - t0)  > 1e-5f ? 1f / (t2 - t0)  : 0f;

            Vector3 m0 = (p1 - p_1) * m0Scale;
            Vector3 m1 = (p2 - p0)  * m1Scale;

            float u2 = u * u;
            float u3 = u2 * u;

            float h00 =  2f*u3 - 3f*u2 + 1f;
            float h10 =      u3 - 2f*u2 + u;
            float h01 = -2f*u3 + 3f*u2;
            float h11 =      u3 -     u2;

            rawPos = h00 * p0 + h10 * (span * m0) + h01 * p1 + h11 * (span * m1);
        }
        else
        {
            rawPos = Vector3.Lerp(f0.position, f1.position, u);
        }

        rawPos += worldPositionOffset;

        Quaternion rawRot;
        if (rotationMode == RotationMode.UseRecordedRotation)
        {
            rawRot = Quaternion.Slerp(f0.rotation, f1.rotation, u);
        }
        else if (rotationMode == RotationMode.FaceVelocity)
        {
            Vector3 v = (f1.position - f0.position) / span;
            if (applyOutputSmoothing && positionSmoothTimeSeconds > 0f && dt > 0f)
                v = (rawPos - smoothedPos) / Mathf.Max(1e-5f, dt);

            rawRot = v.sqrMagnitude > faceVelocityMinSpeed * faceVelocityMinSpeed
                ? Quaternion.LookRotation(v.normalized, Vector3.up)
                : Quaternion.Slerp(f0.rotation, f1.rotation, u);
        }
        else
        {
            rawRot = ghostTransform.rotation;
        }

        Vector3 finalPos = rawPos;
        Quaternion finalRot = rawRot;

        if (applyOutputSmoothing)
        {
            if (positionSmoothTimeSeconds > 0f)
                finalPos = Vector3.SmoothDamp(smoothedPos, rawPos, ref smoothedPosVel, positionSmoothTimeSeconds, Mathf.Infinity, Mathf.Max(0f, dt));
            float rotLerp = rotationSmoothTimeSeconds > 0f ? (1f - Mathf.Exp(-Mathf.Max(0f, dt) / rotationSmoothTimeSeconds)) : 1f;
            finalRot = Quaternion.Slerp(smoothedRot, rawRot, Mathf.Clamp01(rotLerp));
        }

        smoothedPos = finalPos;
        smoothedRot = finalRot;

        ghostTransform.SetPositionAndRotation(finalPos, finalRot);
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
            Gizmos.DrawLine(cachedPositions[i - 1], cachedPositions[i]);
    }

    private void CachePositions()
    {
        cachedPositions.Clear();
        if (!HasValidRun()) return;
        var frames = loadedRun.frames;
        for (int i = 0; i < frames.Count; i++)
            cachedPositions.Add(frames[i].position + worldPositionOffset);
    }
#endif
}