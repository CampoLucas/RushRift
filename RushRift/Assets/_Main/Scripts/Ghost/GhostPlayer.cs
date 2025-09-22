using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GhostPlaybackUtils;

namespace _Main.Scripts.Ghost
{
    [DisallowMultipleComponent]
    public class GhostPlayer : MonoBehaviour
    {
        public enum RotationMode { UseRecordedRotation, FaceVelocity, IgnoreRotation }
        public enum PositionInterpolationMode { Linear, CatmullRom }

        [Header("Ghost Source")]
        [SerializeField] private bool autoLoadOnEnable = true;

        [Header("Ghost Visual")]
        [SerializeField] private GameObject ghostVisualPrefab;
        [SerializeField] private bool parentGhostUnderThis = true;
        [SerializeField] private Vector3 worldPositionOffset = Vector3.zero;

        [Header("Proximity Fade")]
        [SerializeField] private bool enableProximityFade = true;
        [SerializeField] private Material proximityFadeMaterial;
        [SerializeField, Range(0f, 1f)] private float alphaWhenClose = 0.15f;
        [SerializeField, Range(0f, 1f)] private float alphaWhenFar = 0.85f;
        [SerializeField] private float fadeMinDistance = 0.5f;
        [SerializeField] private float fadeMaxDistance = 12f;
        [SerializeField] private string playerTagForFade = "Player";

        [Header("Start Alignment")]
        [SerializeField] private bool alignGhostStartToPlayer = true;
        [SerializeField] private string playerTagForAlignment = "Player";
        [SerializeField] private float maxStartAlignDistance = 0.5f;

        [Header("Playback")]
        [SerializeField] private bool beginPlaybackOnEnable = true;
        [SerializeField] private float playbackSpeed = 1f;
        [SerializeField] private bool loopPlayback;
        [SerializeField] private bool useUnscaledTime;

        [Header("Interpolation")]
        [SerializeField] private PositionInterpolationMode positionInterpolation = PositionInterpolationMode.CatmullRom;
        [SerializeField] private RotationMode rotationMode = RotationMode.UseRecordedRotation;
        [SerializeField] private float faceVelocityMinSpeed = 0.05f;

        [Header("Output Smoothing")]
        [SerializeField] private bool applyOutputSmoothing = true;
        [SerializeField] private float positionSmoothTimeSeconds = 0.06f;
        [SerializeField] private float rotationSmoothTimeSeconds = 0.06f;

        [Header("Visibility")]
        [SerializeField] private bool initialGhostVisible = true;
        [SerializeField] private KeyCode toggleVisibilityKey = KeyCode.G;

        [Header("Pause Integration")]
        [SerializeField] private bool obeyPauseEvents = true;

        [Header("Controls")]
        [SerializeField] private KeyCode togglePlayKey = KeyCode.None;

        [Header("Debug")]
        [SerializeField] private bool isDebugLoggingEnabled;
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private int gizmoMaxSegments = 256;
        [SerializeField] private Color gizmoPathColor = new Color(0f, 1f, 0.6f, 0.85f);
        [SerializeField] private string debugLoadedGhostPath = "";

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
    
        private readonly List<Frame> framesCache = new();
        private ProximityFader _fader;
        private bool _suppressBuildWarnings;

        private void Awake()
        {
            _suppressBuildWarnings |= drawGizmos && gizmoMaxSegments >= 0;
            if (enableProximityFade && proximityFadeMaterial)
                _fader = new ProximityFader(proximityFadeMaterial, playerTagForFade, fadeMinDistance, fadeMaxDistance, alphaWhenClose, alphaWhenFar);
        }

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
            { if (isPlaying) Pause(); else if (!PauseEventBus.IsPaused) Play(); }

            if (toggleVisibilityKey != KeyCode.None && Input.GetKeyDown(toggleVisibilityKey))
                ToggleGhostVisible();

            if (PauseEventBus.IsPaused || !isPlaying || !HasValidRun() || !ghostTransform) return;

            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float dur = Duration(framesCache, loadedRun.durationSeconds);
        
            if (dur <= 0f) return;

            playbackTime += Mathf.Max(0f, dt) * Mathf.Max(0.0001f, playbackSpeed);

            if (playbackTime >= dur)
            {
                if (loopPlayback) { playbackTime %= dur; nextFrameIndex = 1; }
                else { playbackTime = dur; ApplyPose(playbackTime, dt); Pause(); return; }
            }

            if (_fader != null) _fader.Update(ghostTransform.position);
            ApplyPose(playbackTime, dt);
        }

        private void OnPauseChanged(bool paused)
        {
            if (!obeyPauseEvents) return;
            if (paused) { wasPlayingBeforePause = isPlaying; if (isPlaying) Pause(); }
            else if (wasPlayingBeforePause && HasValidRun()) Play();
        }

        public void LoadBestGhost()
        {
            GhostRecorder.GhostRunData data; string path;
            if (GhostRecorder.TryLoadBestGhostForCurrentLevel(out data, out path))
            {
                loadedRun = data;
                debugLoadedGhostPath = path;
            
                framesCache.Clear();
                for (int i = 0; i < loadedRun.frames.Count; i++)
                {
                    var f = loadedRun.frames[i];
                    framesCache.Add(new Frame { time = f.time, position = f.position, rotation = f.rotation });
                }

                CachePositions();
                TrySpawnGhostVisualIfNeeded();
                AlignToPlayerStartIfNeeded();

                var f0 = loadedRun.frames[0];
                smoothedPos = f0.position + worldPositionOffset;
                smoothedRot = f0.rotation;

                Log($"Loaded BEST ghost ({loadedRun.durationSeconds:0.###}s) from: {debugLoadedGhostPath}");
            }
        
            else
            {
                loadedRun = null;
                framesCache.Clear();
                cachedPositions.Clear();
                debugLoadedGhostPath = "";
                Log($"No BEST ghost found for level {SceneManager.GetActiveScene().buildIndex}");
            }

            nextFrameIndex = 1; playbackTime = 0f;
        }

        public void Play() { if (!HasValidRun()) { Log("Play ignored: no run"); return; } EnsureGhostVisual(); isPlaying = true; Log("Play"); }
        public void Pause() { isPlaying = false; Log("Pause"); }
        public void Stop() { isPlaying = false; playbackTime = 0f; nextFrameIndex = 1; if (HasValidRun()) ApplyPose(0f, 0f); Log("Stop"); }

        public void SetGhostVisible(bool visible)
        {
            isGhostVisible = visible;
            if (!ghostTransform) return;

            if (ghostTransform != transform) { ghostTransform.gameObject.SetActive(isGhostVisible); return; }
            if (cachedRenderers.Count == 0 && cachedParticles.Count == 0) CacheVisualComponents();

            foreach (var r in cachedRenderers) if (r) r.enabled = isGhostVisible;
            foreach (var ps in cachedParticles) { if (!ps) continue; if (isGhostVisible) ps.Play(true); else { ps.Pause(true); ps.Clear(true); } }
        }
        public void ToggleGhostVisible() { SetGhostVisible(!isGhostVisible); Log(isGhostVisible ? "Ghost visible" : "Ghost hidden"); }

        private void EnsureGhostVisual()
        {
            if (ghostTransform != null) return;

            if (ghostVisualPrefab && HasValidRun())
            {
                var go = Instantiate(ghostVisualPrefab, Vector3.zero, Quaternion.identity, parentGhostUnderThis ? transform : null);
                ghostTransform = go.transform;
                Log("Spawned ghost visual prefab");
            }
            else
            {
                ghostTransform = transform;
                Log("No valid run available, NOT spawning prefab (using self transform)");
            }

            CacheVisualComponents();
            if (HasValidRun()) ApplyPose(0f, 0f);
        }

        private void TrySpawnGhostVisualIfNeeded()
        {
            bool canSpawnPrefab = ghostVisualPrefab && HasValidRun();
            if (!canSpawnPrefab) return;
            if (ghostTransform != null && ghostTransform != transform) return;

            var go = Instantiate(ghostVisualPrefab, Vector3.zero, Quaternion.identity, parentGhostUnderThis ? transform : null);
            ghostTransform = go.transform;
            CacheVisualComponents();
            if (HasValidRun()) ApplyPose(0f, 0f);
            Log("Spawned ghost visual prefab (run available)");
        }

        private void CacheVisualComponents()
        {
            cachedRenderers.Clear(); cachedParticles.Clear();
            if (!ghostTransform) return;

            if (ghostTransform == transform)
            { cachedRenderers.AddRange(GetComponentsInChildren<Renderer>(true)); cachedParticles.AddRange(GetComponentsInChildren<ParticleSystem>(true)); }
            else
            { cachedRenderers.AddRange(ghostTransform.GetComponentsInChildren<Renderer>(true)); cachedParticles.AddRange(ghostTransform.GetComponentsInChildren<ParticleSystem>(true)); }
        }

        private void AlignToPlayerStartIfNeeded()
        {
            if (!alignGhostStartToPlayer || !HasValidRun()) return;
            var player = GameObject.FindGameObjectWithTag(playerTagForAlignment);
            if (!player) return;

            var first = loadedRun.frames[0].position;
            var desired = player.transform.position;
            Vector3 delta = desired - (first + worldPositionOffset);

            if (delta.sqrMagnitude <= maxStartAlignDistance * maxStartAlignDistance)
            {
                worldPositionOffset += delta;
                smoothedPos = first + worldPositionOffset;
                if (ghostTransform) ghostTransform.position = smoothedPos;
                Log($"Auto-aligned ghost start by {delta.magnitude:0.###}m");
            }
        }

        private bool HasValidRun() =>
            loadedRun is { frames: { Count: >= 2 }, durationSeconds: > 0f };

        private void ApplyPose(float t, float dt)
        {
            var frames = framesCache;

            nextFrameIndex = AdvanceFrameIndex(frames, t, nextFrameIndex);
            int i1 = nextFrameIndex;
            int i0 = i1 - 1;

            Vector3 rawPos = SamplePosition(frames, i0, i1, t, (PosInterp)positionInterpolation) + worldPositionOffset;

            Quaternion rawRot;
        
            if (rotationMode == RotationMode.IgnoreRotation) rawRot = ghostTransform.rotation;
            else rawRot = SampleRotation(frames, i0, i1, t, (RotMode)rotationMode, rawPos, smoothedPos, dt, faceVelocityMinSpeed);

            Vector3 finalPos = applyOutputSmoothing && positionSmoothTimeSeconds > 0f
                ? SmoothPosition(smoothedPos, rawPos, ref smoothedPosVel, positionSmoothTimeSeconds, dt)
                : rawPos;

            Quaternion finalRot = applyOutputSmoothing
                ? SmoothRotation(smoothedRot, rawRot, rotationSmoothTimeSeconds, dt)
                : rawRot;

            smoothedPos = finalPos; smoothedRot = finalRot;
            ghostTransform.SetPositionAndRotation(finalPos, finalRot);
        }
    
        private void Log(string msg)
        {
            if (!isDebugLoggingEnabled) return;
            Debug.Log($"[GhostPlayer] {name}: {msg}", this);
        }

        private void CachePositions()
        {
            cachedPositions.Clear();
            if (!HasValidRun()) return;
            var frames = loadedRun.frames;
            for (int i = 0; i < frames.Count; i++)
                cachedPositions.Add(frames[i].position + worldPositionOffset);
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
#endif
    }
}