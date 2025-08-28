using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class LightBridge : ObserverComponent
{
    public enum LengthAxis { X, Y, Z }
    public enum MeshPivotLocation { AtStart, AtCenter }

    [Header("Endpoints")]
    [SerializeField, Tooltip("Origin (Point A). The bridge starts here.")]
    private Transform originPointA;

    [SerializeField, Tooltip("End (Point B). The bridge grows toward this position.")]
    private Transform endPointB;

    [Header("Visual")]
    [SerializeField, Tooltip("Transform to rotate/scale for the bridge visuals. If null, uses this Transform.")]
    private Transform bridgeVisualRoot;

    [SerializeField, Tooltip("Local axis on the visual that represents length (the axis that will be scaled).")]
    private LengthAxis lengthAxis = LengthAxis.Z;

    [SerializeField, Tooltip("Where the mesh pivot is located along the length axis.")]
    private MeshPivotLocation meshPivotLocation = MeshPivotLocation.AtCenter;

    [SerializeField, Tooltip("How many meters the mesh represents when its length-axis scale is 1.")]
    private float meshLengthAtScaleOneMeters = 1f;

    [SerializeField, Tooltip("If true, rotates the visual so the length axis aligns with parent forward toward B.")]
    private bool alignAxisToForward = true;

    [Header("Deployment")]
    [SerializeField, Tooltip("Seconds to deploy from A to B.")]
    private float deploymentDurationSeconds = 1f;

    [SerializeField, Tooltip("Seconds to retract from B back to A.")]
    private float retractionDurationSeconds = 0.75f;

    [SerializeField, Tooltip("Easing for deployment/retraction (0..1 time → 0..1 progress).")]
    private AnimationCurve easingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [SerializeField, Tooltip("When true, follows moving endpoints during animation.")]
    private bool trackTargetsDuringAnimation = true;

    [SerializeField, Tooltip("If true, starts already deployed (scaled to B).")]
    private bool startDeployed;

    [Header("Observer / Control")]
    [SerializeField, Tooltip("If false, ignores OnNotify and public commands.")]
    private bool canBeUsed = true;

    [SerializeField, Tooltip("Argument that triggers deployment in OnNotify.")]
    private string onArgument = "ON";

    [SerializeField, Tooltip("Argument that triggers retraction in OnNotify.")]
    private string offArgument = "OFF";

    [Header("Debug")]
    [SerializeField, Tooltip("If enabled, prints detailed logs.")]
    private bool isDebugLoggingEnabled;

    [SerializeField, Tooltip("Draw gizmos for endpoints and current tip.")]
    private bool drawGizmos = true;
    
    private Coroutine animationCoroutine;
    private Vector3 baseLocalScale;
    private float currentProgress01; // 0..1

    private void Reset()
    {
        if (!bridgeVisualRoot) bridgeVisualRoot = transform;
        meshLengthAtScaleOneMeters = Mathf.Max(0.0001f, meshLengthAtScaleOneMeters);
        Log("Reset complete.");
    }

    private void Awake()
    {
        if (!bridgeVisualRoot) bridgeVisualRoot = transform;
        if (!originPointA || !endPointB)
            Debug.LogWarning($"[LightBridge] Assign both endpoints on '{name}'.", this);

        baseLocalScale = bridgeVisualRoot.localScale;

        AlignToEndpoints();
        SetProgressImmediate(startDeployed ? 1f : 0f);
    }
    
    public void Deploy()
    {
        if (!canBeUsed) { Log("Deploy() ignored (canBeUsed=false)"); return; }
        StartProgressAnimation(1f, deploymentDurationSeconds);
    }

    public void Retract()
    {
        if (!canBeUsed) { Log("Retract() ignored (canBeUsed=false)"); return; }
        StartProgressAnimation(0f, retractionDurationSeconds);
    }

    public override void OnNotify(string arg)
    {
        if (!canBeUsed) { Log($"OnNotify('{arg}') ignored (canBeUsed=false)"); return; }
        string a = (arg ?? "").Trim().ToUpperInvariant();
        
        if (a == onArgument.ToUpperInvariant() || a == "ON" || a == "OPEN" || a == "ENABLE") Deploy();
        else if (a == offArgument.ToUpperInvariant() || a == "OFF" || a == "CLOSE" || a == "DISABLE") Retract();
        else Log($"OnNotify('{arg}') did not match any action.");
    }
    
    private void StartProgressAnimation(float target, float duration)
    {
        if (!originPointA || !endPointB) { Log("Cannot animate: endpoints not set."); return; }
        if (Mathf.Approximately(currentProgress01, target)) { Log("Already at target progress."); return; }

        if (animationCoroutine != null) StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(AnimateTo(target, Mathf.Max(0f, duration)));
    }

    private IEnumerator AnimateTo(float target01, float duration)
    {
        float start = currentProgress01;
        float t = 0f;

        while (t < 1f)
        {
            if (trackTargetsDuringAnimation) AlignToEndpoints();

            t += (duration > 0f ? Time.deltaTime / duration : 1f);
            float eased = easingCurve.Evaluate(Mathf.Clamp01(t));
            ApplyProgress(Mathf.Lerp(start, target01, eased));
            yield return null;
        }

        ApplyProgress(target01);
        animationCoroutine = null;
        Log($"Animation finished. progress={currentProgress01:0.###}");
    }

    private void SetProgressImmediate(float p) => ApplyProgress(Mathf.Clamp01(p));

    private void ApplyProgress(float p01)
    {
        currentProgress01 = Mathf.Clamp01(p01);

        // Keep root at A and facing B
        AlignToEndpoints();

        // Current desired length along A→B
        float fullLen = GetDistanceAB();
        float currLen = fullLen * currentProgress01;

        // Scale only along the chosen axis
        Vector3 s = baseLocalScale;
        float baseAxisScale = GetAxis(s);
        float axisScale = baseAxisScale * (currLen / Mathf.Max(0.0001f, meshLengthAtScaleOneMeters));
        SetAxis(ref s, Mathf.Max(0f, axisScale));
        bridgeVisualRoot.localScale = s;

        // Position compensation so the bridge extrudes from A → B:
        // - If the mesh pivot is at the START, we leave localPosition at 0.
        // - If the mesh pivot is at the CENTER, we offset the visual forward by half the current length,
        //   so the "back" end stays pinned at A and only the tip moves toward B.
        Vector3 localLenAxis = alignAxisToForward ? Vector3.forward : AxisVector(lengthAxis);
        localLenAxis = localLenAxis.normalized;

        if (meshPivotLocation == MeshPivotLocation.AtCenter)
            bridgeVisualRoot.localPosition = localLenAxis * (currLen * 0.5f);
        else
            bridgeVisualRoot.localPosition = Vector3.zero;
    }
    
    private void AlignToEndpoints()
    {
        if (!originPointA || !endPointB) return;

        Vector3 a = originPointA.position;
        Vector3 b = endPointB.position;
        Vector3 dir = (b - a);
        if (dir.sqrMagnitude < 1e-10f) dir = transform.forward;

        transform.SetPositionAndRotation(a, Quaternion.LookRotation(dir.normalized, Vector3.up));

        if (alignAxisToForward && bridgeVisualRoot)
        {
            Quaternion rot = Quaternion.FromToRotation(AxisVector(lengthAxis), Vector3.forward);
            bridgeVisualRoot.localRotation = rot;
        }
    }

    private float GetDistanceAB()
    {
        if (!originPointA || !endPointB) return 0f;
        return Vector3.Distance(originPointA.position, endPointB.position);
    }
    
    private static Vector3 AxisVector(LengthAxis axis) =>
        axis == LengthAxis.X ? Vector3.right : axis == LengthAxis.Y ? Vector3.up : Vector3.forward;

    private static float GetAxis(Vector3 v, LengthAxis axis) =>
        axis == LengthAxis.X ? v.x : axis == LengthAxis.Y ? v.y : v.z;

    private float GetAxis(Vector3 v) => GetAxis(v, lengthAxis);

    private static void SetAxis(ref Vector3 v, float value, LengthAxis axis)
    {
        if (axis == LengthAxis.X) v.x = value;
        else if (axis == LengthAxis.Y) v.y = value;
        else v.z = value;
    }

    private void SetAxis(ref Vector3 v, float value) => SetAxis(ref v, value, lengthAxis);
    
    private void Log(string msg)
    {
        if (!isDebugLoggingEnabled) return;
        Debug.Log($"[LightBridge] {name}: {msg}", this);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        
        if (originPointA && endPointB)
        {
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.9f);
            Gizmos.DrawLine(originPointA.position, endPointB.position);
            Gizmos.DrawSphere(originPointA.position, 0.04f);
            Gizmos.DrawSphere(endPointB.position, 0.04f);

            if (Application.isPlaying)
            {
                Vector3 tip = Vector3.Lerp(originPointA.position, endPointB.position, Mathf.Clamp01(currentProgress01));
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(tip, 0.035f);
            }
        }
    }
#endif
}