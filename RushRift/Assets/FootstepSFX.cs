using UnityEngine;

[DisallowMultipleComponent]
public class FootstepSFX : MonoBehaviour
{
    [System.Serializable]
    private struct SurfaceSound
    {
        [Tooltip("Collider/Renderer tag to match underfoot")]
        public string tag;
        [Tooltip("AudioManager Sound name to play for this surface")]
        public string soundName;
    }

    [Header("Audio")]
    [SerializeField, Tooltip("Default AudioManager sound to play for footsteps (use a Sound with multiple clips for variation).")]
    private string defaultFootstepSound = "Footstep";
    [SerializeField, Tooltip("Landing sound when touching ground after a fall (optional).")]
    private string landingSound = "Landing";
    [SerializeField, Tooltip("Optional per-surface overrides by tag (e.g., Grass, Metal, Stone).")]
    private SurfaceSound[] surfaceOverrides;

    [Header("Cadence")]
    [SerializeField, Tooltip("Meters per step when walking.")]
    private float stepMetersWalk = 0.6f;
    [SerializeField, Tooltip("Meters per step when running at or above Top Speed.")]
    private float stepMetersRun = 0.8f;
    [SerializeField, Tooltip("Horizontal speed in m/s considered 'top speed' for step scaling.")]
    private float topSpeedMetersPerSecond = 8f;
    [SerializeField, Tooltip("Minimum horizontal speed required to start stepping.")]
    private float minSpeedToStep = 1.5f;

    [Header("Grounding")]
    [SerializeField, Tooltip("How far down to raycast for ground checks (from this transform).")]
    private float groundRayDistance = 1.2f;
    [SerializeField, Tooltip("LayerMask for ground surfaces.")]
    private LayerMask groundMask = ~0;
    [SerializeField, Tooltip("Extra sphere cast radius to support uneven terrain.")]
    private float groundProbeRadius = 0.08f;

    [Header("Landing")]
    [SerializeField, Tooltip("Min downward speed on touchdown to play the landing sound.")]
    private float minFallSpeedForLanding = 4f;

    [Header("Debug")]
    [SerializeField, Tooltip("If enabled, prints detailed logs.")]
    private bool isDebugLoggingEnabled = false;
    [SerializeField, Tooltip("Draw probe gizmos.")]
    private bool drawGizmos = true;

    private Rigidbody _rb;
    private Transform _tf;

    private Vector3 _prevPos;
    private float _accumulatedMeters;
    private bool _wasGrounded;
    private float _lastGroundedYVel;

    private void Awake()
    {
        _rb = GetComponentInParent<Rigidbody>();
        _tf = transform;
        _prevPos = _tf.position;
    }

    private void Update()
    {
        bool grounded = ProbeGround(out var hit);
        Vector3 pos = _tf.position;
        Vector3 delta = pos - _prevPos;
        _prevPos = pos;

        Vector3 horizDelta = new Vector3(delta.x, 0f, delta.z);
        float horizSpeed = horizDelta.magnitude / Mathf.Max(Time.deltaTime, 1e-5f);

        if (grounded)
        {
            if (!_wasGrounded)
            {
                float fallSpeed = Mathf.Abs(_lastGroundedYVel);
                if (!string.IsNullOrEmpty(landingSound) && fallSpeed >= minFallSpeedForLanding)
                    Play(landingSound);
            }

            if (horizSpeed >= minSpeedToStep)
            {
                _accumulatedMeters += horizDelta.magnitude;

                float speed01 = Mathf.Clamp01(horizSpeed / Mathf.Max(0.01f, topSpeedMetersPerSecond));
                float stepMeters = Mathf.Lerp(stepMetersWalk, stepMetersRun, speed01);

                if (_accumulatedMeters >= stepMeters)
                {
                    _accumulatedMeters = 0f;
                    Play(ResolveSurfaceSound(hit));
                }
            }
            else
            {
                _accumulatedMeters = 0f;
            }

            _lastGroundedYVel = _rb ? Mathf.Min(0f, _rb.velocity.y) : 0f;
        }

        _wasGrounded = grounded;
    }

    private string ResolveSurfaceSound(in RaycastHit hit)
    {
        if (hit.collider)
        {
            var tag = hit.collider.tag;
            for (int i = 0; i < surfaceOverrides.Length; i++)
            {
                if (!string.IsNullOrEmpty(surfaceOverrides[i].tag) &&
                    surfaceOverrides[i].tag == tag &&
                    !string.IsNullOrEmpty(surfaceOverrides[i].soundName))
                {
                    return surfaceOverrides[i].soundName;
                }
            }
        }
        return defaultFootstepSound;
    }

    private bool ProbeGround(out RaycastHit hit)
    {
        Vector3 origin = _tf.position + Vector3.up * 0.05f;
        float dist = groundRayDistance + 0.05f;

        // sphere cast is more forgiving on corners/ledges
        bool grounded = Physics.SphereCast(origin, groundProbeRadius, Vector3.down, out hit, dist, groundMask, QueryTriggerInteraction.Ignore);
        if (!grounded)
        {
            grounded = Physics.Raycast(origin, Vector3.down, out hit, dist, groundMask, QueryTriggerInteraction.Ignore);
        }
        return grounded;
    }

    private void Play(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        Game.AudioManager.Play(soundName); // uses your AudioManager & Sound randomization
        if (isDebugLoggingEnabled) Debug.Log($"[FootstepSFX] {name}: {soundName}", this);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        var p = transform.position + Vector3.up * 0.05f;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(p, groundProbeRadius);
        Gizmos.DrawLine(p, p + Vector3.down * groundRayDistance);
    }
#endif
}
