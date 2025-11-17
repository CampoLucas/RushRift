using System.Collections;
using UnityEngine;

[AddComponentMenu("Camera/Camera Wiggle")]
public class CameraWiggle : MonoBehaviour
{
    public enum WiggleMode { PerlinNoise, CurvePingPong }

    [Header("Status")]
    [Tooltip("Enable to automatically start wiggling when the object is enabled.")]
    [SerializeField] private bool playOnEnable = true;
    [Tooltip("Use unscaled time for wiggle updates.")]
    [SerializeField] private bool useUnscaledTime = true;
    [Tooltip("Master enable for the wiggle.")]
    [SerializeField] private bool wiggleEnabled = true;

    [Header("Position Wiggle")]
    [Tooltip("Enable position wiggling.")]
    [SerializeField] private bool positionEnabled = true;
    [Tooltip("Position wiggle mode.")]
    [SerializeField] private WiggleMode positionMode = WiggleMode.PerlinNoise;
    [Tooltip("Position amplitude in local space.")]
    [SerializeField] private Vector3 positionAmplitude = new Vector3(0.05f, 0.05f, 0.05f);
    [Tooltip("Position frequency in Hz-like units.")]
    [SerializeField] private float positionFrequency = 1.5f;
    [Tooltip("Position curve evaluated 0→1 for CurvePingPong mode.")]
    [SerializeField] private AnimationCurve positionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Rotation Wiggle")]
    [Tooltip("Enable rotation wiggling.")]
    [SerializeField] private bool rotationEnabled = true;
    [Tooltip("Rotation wiggle mode.")]
    [SerializeField] private WiggleMode rotationMode = WiggleMode.PerlinNoise;
    [Tooltip("Rotation amplitude in degrees.")]
    [SerializeField] private Vector3 rotationAmplitude = new Vector3(0.5f, 0.5f, 0.5f);
    [Tooltip("Rotation frequency in Hz-like units.")]
    [SerializeField] private float rotationFrequency = 1.25f;
    [Tooltip("Rotation curve evaluated 0→1 for CurvePingPong mode.")]
    [SerializeField] private AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Timing")]
    [Tooltip("Global speed multiplier applied to all wiggles.")]
    [SerializeField] private float timeMultiplier = 1f;

    [Header("Debug")]
    [Tooltip("Enable debug logs.")]
    [SerializeField] private bool debugLogs = false;

    [Header("Gizmos")]
    [Tooltip("Draw gizmos to visualize wiggle bounds.")]
    [SerializeField] private bool drawGizmos = true;
    [Tooltip("Gizmo color.")]
    [SerializeField] private Color gizmoColor = new Color(1f, 0.9f, 0.2f, 0.75f);
    [Tooltip("Scale factor for gizmo radius.")]
    [SerializeField] private float gizmoScale = 1f;

    private bool _isActive;
    private Vector3 _baseLocalPosition;
    private Vector3 _baseLocalEulerAngles;
    private float _timePositionX;
    private float _timePositionY;
    private float _timePositionZ;
    private float _timeRotationX;
    private float _timeRotationY;
    private float _timeRotationZ;
    private Coroutine _durationRoutine;

    private void OnEnable()
    {
        RebaseFromCurrentTransform();
        if (playOnEnable) StartWiggle();
    }

    private void OnDisable()
    {
        StopWiggle();
        transform.localPosition = _baseLocalPosition;
        transform.localEulerAngles = _baseLocalEulerAngles;
    }

    private void LateUpdate()
    {
        if (!wiggleEnabled || !_isActive) return;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        dt *= Mathf.Max(0f, timeMultiplier);

        Vector3 posOffset = Vector3.zero;
        Vector3 rotOffset = Vector3.zero;

        if (positionEnabled)
        {
            if (positionMode == WiggleMode.PerlinNoise)
            {
                _timePositionX += dt * positionFrequency;
                _timePositionY += dt * positionFrequency * 1.07f;
                _timePositionZ += dt * positionFrequency * 0.93f;
                posOffset.x = (Mathf.PerlinNoise(_timePositionX, 0.37f) * 2f - 1f) * positionAmplitude.x;
                posOffset.y = (Mathf.PerlinNoise(_timePositionY, 0.73f) * 2f - 1f) * positionAmplitude.y;
                posOffset.z = (Mathf.PerlinNoise(_timePositionZ, 0.19f) * 2f - 1f) * positionAmplitude.z;
            }
            else
            {
                float t = Mathf.PingPong(TimeForCurve(dt, ref _timePositionX, positionFrequency), 1f);
                float e = SafeEvaluate(positionCurve, t);
                posOffset = Vector3.Scale(positionAmplitude, new Vector3(e, e, e));
            }
        }

        if (rotationEnabled)
        {
            if (rotationMode == WiggleMode.PerlinNoise)
            {
                _timeRotationX += dt * rotationFrequency * 1.11f;
                _timeRotationY += dt * rotationFrequency * 0.89f;
                _timeRotationZ += dt * rotationFrequency * 1.03f;
                rotOffset.x = (Mathf.PerlinNoise(_timeRotationX, 0.11f) * 2f - 1f) * rotationAmplitude.x;
                rotOffset.y = (Mathf.PerlinNoise(_timeRotationY, 0.29f) * 2f - 1f) * rotationAmplitude.y;
                rotOffset.z = (Mathf.PerlinNoise(_timeRotationZ, 0.57f) * 2f - 1f) * rotationAmplitude.z;
            }
            else
            {
                float t = Mathf.PingPong(TimeForCurve(dt, ref _timeRotationX, rotationFrequency), 1f);
                float e = SafeEvaluate(rotationCurve, t);
                rotOffset = Vector3.Scale(rotationAmplitude, new Vector3(e, e, e));
            }
        }

        transform.localPosition = _baseLocalPosition + posOffset;
        transform.localEulerAngles = _baseLocalEulerAngles + rotOffset;
    }

    public void StartWiggle()
    {
        _isActive = true;
        if (debugLogs) Debug.Log("[CameraWiggle] StartWiggle");
    }

    public void StopWiggle()
    {
        if (_durationRoutine != null)
        {
            StopCoroutine(_durationRoutine);
            _durationRoutine = null;
        }
        _isActive = false;
        if (debugLogs) Debug.Log("[CameraWiggle] StopWiggle");
    }

    public void TriggerWiggleForSeconds(float duration)
    {
        if (_durationRoutine != null) StopCoroutine(_durationRoutine);
        _durationRoutine = StartCoroutine(TriggerRoutine(duration));
    }

    public void RebaseFromCurrentTransform()
    {
        _baseLocalPosition = transform.localPosition;
        _baseLocalEulerAngles = transform.localEulerAngles;
        if (debugLogs) Debug.Log("[CameraWiggle] RebaseFromCurrentTransform");
    }

    private IEnumerator TriggerRoutine(float duration)
    {
        StartWiggle();
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, duration));
        StopWiggle();
        transform.localPosition = _baseLocalPosition;
        transform.localEulerAngles = _baseLocalEulerAngles;
    }

    private static float SafeEvaluate(AnimationCurve curve, float t)
    {
        if (curve == null || curve.length == 0) return t;
        return Mathf.Clamp01(curve.Evaluate(Mathf.Clamp01(t)));
    }

    private static float TimeForCurve(float dt, ref float acc, float frequency)
    {
        float f = Mathf.Max(0f, frequency);
        acc += dt * f;
        return acc;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Gizmos.color = gizmoColor;
        float r = (positionAmplitude.magnitude + (rotationAmplitude.magnitude * 0.01f)) * Mathf.Max(0.01f, gizmoScale);
        Gizmos.DrawWireSphere(transform.position, r);
        Gizmos.DrawLine(transform.position, transform.position + transform.right * r * 0.5f);
        Gizmos.DrawLine(transform.position, transform.position + transform.up * r * 0.5f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * r * 0.5f);
    }
}