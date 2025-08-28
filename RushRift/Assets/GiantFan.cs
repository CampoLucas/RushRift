using UnityEngine;
using Game.LevelElements.Terminal;

[DisallowMultipleComponent]
public class GiantFan : ObserverComponent
{
    public enum DirectionMode { Normal, Reversed }

    [Header("Usage / Control")]
    [SerializeField, Tooltip("If false, ignores commands and does nothing.")]
    private bool canBeUsed = true;

    [SerializeField, Tooltip("Turn the fan on at Start.")]
    private bool turnOnAtStart = false;

    [SerializeField, Tooltip("Argument that enables the fan in OnNotify.")]
    private string onArgument = "ON";

    [SerializeField, Tooltip("Argument that disables the fan in OnNotify.")]
    private string offArgument = "OFF";

    [Header("Rotation")]
    [SerializeField, Tooltip("Transform that rotates. If null, this Transform is used.")]
    private Transform rotorRoot;

    [SerializeField, Tooltip("Local axis to rotate around.")]
    private Vector3 localRotationAxis = Vector3.up;

    [SerializeField, Tooltip("Target speed when ON, in RPM.")]
    [Min(0f)] private float targetSpeedRpm = 120f;

    [SerializeField, Tooltip("Direction of rotation.")]
    private DirectionMode direction = DirectionMode.Normal;

    [SerializeField, Tooltip("Seconds to ramp from 0 to target speed.")]
    [Min(0f)] private float rampUpSeconds = 1f;

    [SerializeField, Tooltip("Seconds to ramp from target to 0.")]
    [Min(0f)] private float rampDownSeconds = 0.6f;

    [SerializeField, Tooltip("Use unscaled time for rotation and ramps.")]
    private bool useUnscaledTime = false;

    [Header("Debug")]
    [SerializeField, Tooltip("If enabled, prints detailed logs.")]
    private bool isDebugLoggingEnabled = false;

    [SerializeField, Tooltip("Draw gizmos for axis and approximate radius.")]
    private bool drawGizmos = true;

    [SerializeField, Tooltip("Gizmo radius visualization.")]
    [Min(0f)] private float gizmoRadius = 0.75f;

    private bool isOn;
    private float currentSpeedDegPerSec;
    private float desiredSpeedDegPerSec;

    private void Awake()
    {
        if (!rotorRoot) rotorRoot = transform;
        localRotationAxis = localRotationAxis.sqrMagnitude < 1e-6f ? Vector3.up : localRotationAxis.normalized;
        isOn = false;
        currentSpeedDegPerSec = 0f;
        desiredSpeedDegPerSec = 0f;
    }

    private void Start()
    {
        if (turnOnAtStart) TurnOn();
    }

    private void Update()
    {
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float signedTargetDeg = RpmToDegPerSec(targetSpeedRpm) * (direction == DirectionMode.Reversed ? -1f : 1f);
        desiredSpeedDegPerSec = isOn ? signedTargetDeg : 0f;

        float ramp = 0f;
        if (Mathf.Approximately(desiredSpeedDegPerSec, 0f))
            ramp = rampDownSeconds <= 0f ? Mathf.Infinity : Mathf.Abs(currentSpeedDegPerSec) / rampDownSeconds;
        else
            ramp = rampUpSeconds <= 0f ? Mathf.Infinity : Mathf.Abs(signedTargetDeg) / rampUpSeconds;

        currentSpeedDegPerSec = Mathf.MoveTowards(currentSpeedDegPerSec, desiredSpeedDegPerSec, ramp * dt);

        if (Mathf.Abs(currentSpeedDegPerSec) > 0f && rotorRoot)
            rotorRoot.Rotate(localRotationAxis, currentSpeedDegPerSec * dt, Space.Self);
    }

    public override void OnNotify(string arg)
    {
        if (!canBeUsed) { Log($"OnNotify ignored: {arg}"); return; }
        string a = (arg ?? "").Trim().ToUpperInvariant();
        bool isOnArg = a == onArgument.ToUpperInvariant() || a == Terminal.ON_ARGUMENT || a == "ON" || a == "ENABLE";
        bool isOffArg = a == offArgument.ToUpperInvariant() || a == Terminal.OFF_ARGUMENT || a == "OFF" || a == "DISABLE";
        if (isOnArg) TurnOn();
        else if (isOffArg) TurnOff();
        else Log($"OnNotify no match: {arg}");
    }

    public void TurnOn()
    {
        if (!canBeUsed) { Log("TurnOn ignored"); return; }
        if (isOn) { Log("Already ON"); return; }
        isOn = true;
        Log("ON");
    }

    public void TurnOff()
    {
        if (!canBeUsed) { Log("TurnOff ignored"); return; }
        if (!isOn) { Log("Already OFF"); return; }
        isOn = false;
        Log("OFF");
    }

    public void Toggle()
    {
        if (!canBeUsed) { Log("Toggle ignored"); return; }
        if (isOn) TurnOff(); else TurnOn();
    }

    public void SetDirection(DirectionMode newDirection)
    {
        direction = newDirection;
        Log($"Direction: {direction}");
    }

    public void SetTargetRpm(float rpm)
    {
        targetSpeedRpm = Mathf.Max(0f, rpm);
        Log($"Target RPM: {targetSpeedRpm:0.##}");
    }

    private static float RpmToDegPerSec(float rpm) => rpm * 360f / 60f;

    private void Log(string msg)
    {
        if (!isDebugLoggingEnabled) return;
        Debug.Log($"[GiantFan] {name}: {msg}", this);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Vector3 axis = localRotationAxis.sqrMagnitude < 1e-6f ? Vector3.up : localRotationAxis.normalized;
        Transform t = rotorRoot ? rotorRoot : transform;
        Vector3 p = t.position;
        Vector3 aWorld = t.TransformDirection(axis);
        Gizmos.color = isOn ? new Color(0f, 1f, 0.6f, 0.9f) : new Color(0.6f, 0.6f, 0.6f, 0.9f);
        Gizmos.DrawRay(p, aWorld * 0.5f);
        Gizmos.color = new Color(0f, 1f, 0.6f, 0.35f);
        int steps = 32;
        Vector3 right = Vector3.Cross(aWorld, Vector3.up);
        if (right.sqrMagnitude < 1e-6f) right = Vector3.Cross(aWorld, Vector3.right);
        right.Normalize();
        Vector3 forward = Vector3.Cross(aWorld, right);
        forward.Normalize();
        Vector3 prev = p + (right * gizmoRadius);
        for (int i = 1; i <= steps; i++)
        {
            float ang = (i / (float)steps) * Mathf.PI * 2f;
            Vector3 pt = p + (right * Mathf.Cos(ang) + forward * Mathf.Sin(ang)) * gizmoRadius;
            Gizmos.DrawLine(prev, pt);
            prev = pt;
        }
    }
#endif
}
