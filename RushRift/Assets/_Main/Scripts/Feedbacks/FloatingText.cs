using UnityEngine;

namespace _Main.Scripts.Feedbacks
{
    [DisallowMultipleComponent]
    public class FloatingText : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Target TextMeshPro component. If null, the component is searched on this GameObject.")]
        private TextMesh targetText;
        [SerializeField, Tooltip("Optional root for visual transform (scaling, facing). Defaults to this transform.")]
        private Transform visualRoot;

        [Header("Playback")]
        [SerializeField, Tooltip("Total lifetime in seconds once played.")]
        private float totalLifetimeSeconds = 0.8f;
        [SerializeField, Tooltip("Use unscaled time for animation.")]
        private bool useUnscaledTime = true;

        [Header("Motion")]
        [SerializeField, Tooltip("World-space rise distance applied over the lifetime.")]
        private float riseDistanceMeters = 1.2f;
        [SerializeField, Tooltip("Horizontal jitter in meters applied at spawn.")]
        private Vector2 horizontalJitterXZ = new Vector2(0.15f, 0.15f);
        [SerializeField, Tooltip("Optional base direction added to the motion (normalized automatically).")]
        private Vector3 baseDirection = Vector3.up;
        [SerializeField, Tooltip("Curve controlling vertical rise; X=time 0..1, Y=normalized distance.")]
        private AnimationCurve riseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Facing")]
        [SerializeField, Tooltip("If enabled, the text will always face the camera.")]
        private bool alwaysFaceCamera = true;
        [SerializeField, Tooltip("Camera to face. If null and facing is enabled, Camera.main is used.")]
        private Camera cameraToFace;

        [Header("Visuals")]
        [SerializeField, Tooltip("Curve controlling alpha over lifetime; X=time 0..1, Y=alpha 0..1.")]
        private AnimationCurve alphaCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.15f, 1f), new Keyframe(0.75f, 1f), new Keyframe(1, 0));
        [SerializeField, Tooltip("Curve controlling scale over lifetime; X=time 0..1, Y=scale multiplier.")]
        private AnimationCurve scaleCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.2f, 1f), new Keyframe(0.9f, 1f), new Keyframe(1, 0));
        [SerializeField, Tooltip("Optional color gradient animated over lifetime. If empty, base color is used with alpha curve only.")]
        private Gradient colorGradient;

        [Header("Attachment")]
        [SerializeField, Tooltip("If assigned, the text will follow this target while alive.")]
        private Transform followTarget;
        [SerializeField, Tooltip("Offset from the follow target position.")]
        private Vector3 followOffset = Vector3.up * 1f;

        [Header("Debug")]
        [SerializeField, Tooltip("If enabled, prints detailed logs.")]
        private bool isDebugLoggingEnabled = false;
        [SerializeField, Tooltip("Draw gizmos for spawn and target following.")]
        private bool drawGizmos = true;

        private Vector3 spawnWorldPosition;
        private Vector3 spawnHorizontalJitter;
        private Vector3 normalizedBaseDirection;
        private float elapsed;
        private float initialTextScale;
        private Color baseTextColor;
        private bool isPlaying;
        private System.Action<FloatingText> onDespawn;

        /// <summary>Initializes component references.</summary>
        private void Awake()
        {
            if (!targetText) targetText = GetComponent<TextMesh>();
            if (!visualRoot) visualRoot = transform;
            if (targetText) baseTextColor = targetText.color;
            initialTextScale = visualRoot.localScale.x;
            if (!cameraToFace && alwaysFaceCamera) cameraToFace = Camera.main;
        }

        /// <summary>Begins playing the floating animation with the specified parameters.</summary>
        public void Play(string text, Vector3 worldPosition, Vector3 direction, float intensity, float lifetime, Gradient gradient, Transform attachTarget, System.Action<FloatingText> despawnCallback)
        {
            if (!targetText) return;

            onDespawn = despawnCallback;
            spawnWorldPosition = worldPosition;
            followTarget = attachTarget;
            normalizedBaseDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : baseDirection.normalized;
            totalLifetimeSeconds = Mathf.Max(0.01f, lifetime > 0f ? lifetime : totalLifetimeSeconds);
            spawnHorizontalJitter = new Vector3(
                Random.Range(-horizontalJitterXZ.x, horizontalJitterXZ.x),
                0f,
                Random.Range(-horizontalJitterXZ.y, horizontalJitterXZ.y)
            );

            if (gradient != null && gradient.colorKeys != null && gradient.colorKeys.Length > 0)
                colorGradient = gradient;

            targetText.text = text;
            elapsed = 0f;
            isPlaying = true;
            if (isDebugLoggingEnabled) Debug.Log($"[FloatingText] Play at {worldPosition} dir={normalizedBaseDirection} life={totalLifetimeSeconds:0.###}", this);

            ApplyAtTime(0f);
            gameObject.SetActive(true);
        }

        /// <summary>Stops the animation immediately and despawns this instance.</summary>
        public void StopNow()
        {
            isPlaying = false;
            if (resetToInitialOnStop) ResetVisuals();
            onDespawn?.Invoke(this);
        }

        /// <summary>Sets whether this text uses unscaled time, optionally applying it immediately.</summary>
        public void SetUseUnscaledTime(bool unscaled, bool applyImmediately = false)
        {
            useUnscaledTime = unscaled;
            if (applyImmediately) ApplyAtTime(elapsed);
        }

        /// <summary>Assigns an attachment target and optional offset to follow during lifetime.</summary>
        public void SetAttachment(Transform target, Vector3 offset)
        {
            followTarget = target;
            followOffset = offset;
        }

        /// <summary>Updates animation each frame.</summary>
        private void Update()
        {
            if (!isPlaying) return;
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            if (dt <= 0f) return;
            elapsed += dt;

            float t = Mathf.Clamp01(elapsed / totalLifetimeSeconds);
            ApplyAtTime(t);

            if (elapsed >= totalLifetimeSeconds)
            {
                isPlaying = false;
                onDespawn?.Invoke(this);
            }
        }

        /// <summary>Applies transform, color, alpha, and scale at a normalized time.</summary>
        private void ApplyAtTime(float t01)
        {
            var origin = followTarget ? followTarget.position + followOffset : spawnWorldPosition;
            float rise = Mathf.Max(0f, riseDistanceMeters) * Mathf.Clamp01(riseCurve.Evaluate(t01));
            Vector3 displacement = normalizedBaseDirection * rise + spawnHorizontalJitter;

            transform.position = origin + displacement;

            if (alwaysFaceCamera && cameraToFace)
            {
                var camFwd = cameraToFace.transform.forward;
                if (camFwd.sqrMagnitude > 0.0001f)
                    visualRoot.rotation = Quaternion.LookRotation(camFwd, Vector3.up);
            }

            float scaleMul = Mathf.Max(0f, scaleCurve.Evaluate(t01));
            var s = initialTextScale * scaleMul;
            visualRoot.localScale = new Vector3(s, s, s);

            float a = Mathf.Clamp01(alphaCurve.Evaluate(t01));
            if (colorGradient != null && colorGradient.colorKeys != null && colorGradient.colorKeys.Length > 0)
            {
                var c = colorGradient.Evaluate(t01);
                c.a *= a;
                targetText.color = c;
            }
            else
            {
                var c = baseTextColor;
                c.a = a;
                targetText.color = c;
            }
        }

        /// <summary>Resets visuals to their initial state.</summary>
        private void ResetVisuals()
        {
            if (!targetText) return;
            targetText.color = baseTextColor;
            visualRoot.localScale = Vector3.one * initialTextScale;
        }

        /// <summary>Forces a new camera reference for billboarding.</summary>
        public void SetCamera(Camera cam) => cameraToFace = cam;

        /// <summary>Enables or disables facing the camera.</summary>
        public void SetAlwaysFaceCamera(bool enabled) => alwaysFaceCamera = enabled;

        [SerializeField, Tooltip("If true, StopNow() resets visuals to initial values.")]
        private bool resetToInitialOnStop = true;

        private void OnDisable()
        {
            if (!resetToInitialOnStop) return;
            ResetVisuals();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos) return;
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.8f);
            Gizmos.DrawWireSphere(followTarget ? followTarget.position + followOffset : spawnWorldPosition, 0.075f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, Vector3.up * 0.25f);
        }
#endif
    }
}
