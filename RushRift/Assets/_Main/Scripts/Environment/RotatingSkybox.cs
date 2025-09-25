using System.Collections;
using System.Reflection;
using UnityEngine;

[DisallowMultipleComponent]
public class RotatingSkybox : MonoBehaviour
{
    [Header("Target Skybox")]
    [SerializeField, Tooltip("Skybox material to control. If null, uses RenderSettings.skybox.")]
    private Material targetSkyboxMaterial;
    [SerializeField, Tooltip("Duplicate the skybox material at runtime to avoid editing the shared asset.")]
    private bool instantiateSkyboxMaterial = true;
    [SerializeField, Tooltip("Shader property used for rotation.")]
    private string rotationPropertyName = "_Rotation";

    [Header("Rotation")]
    [SerializeField, Tooltip("Degrees per second to rotate.")]
    private float rotationSpeedDegreesPerSecond = 2f;
    [SerializeField, Tooltip("Use unscaled time for rotation.")]
    private bool useUnscaledTime = false;
    [SerializeField, Tooltip("Start rotating automatically on OnEnable.")]
    private bool autoStartOnEnable = true;
    [SerializeField, Tooltip("Read current material rotation on start and continue from there.")]
    private bool captureInitialRotationOnStart = true;
    [SerializeField, Tooltip("Initial angle to apply if not capturing from the material.")]
    private float initialAngleDegrees = 0f;

    [Header("Environment Updates")]
    [SerializeField, Tooltip("Periodically refresh environment lighting/reflections while rotating, if supported by this Unity build.")]
    private bool updateDynamicGI = false;
    [SerializeField, Tooltip("Seconds between environment refresh attempts.")]
    private float dynamicGIUpdateIntervalSeconds = 0.5f;

    [Header("Debug")]
    [SerializeField, Tooltip("If enabled, prints detailed logs.")]
    private bool isDebugLoggingEnabled = false;
    [SerializeField, Tooltip("Draw a small gizmo showing the rotation axis.")]
    private bool drawGizmos = true;

    private Material runtimeMaterial;
    private bool isRunning;
    private float currentAngle;
    private Coroutine giCoroutine;

    private static System.Action tryUpdateEnvironment;

    private void Awake()
    {
        if (tryUpdateEnvironment == null)
        {
            var type = System.Type.GetType("UnityEngine.Rendering.DynamicGI, UnityEngine.CoreModule");
            if (type != null)
            {
                var method = type.GetMethod("UpdateEnvironment", BindingFlags.Public | BindingFlags.Static);
                if (method != null)
                    tryUpdateEnvironment = (System.Action)System.Delegate.CreateDelegate(typeof(System.Action), method);
            }
        }
    }

    private void OnEnable()
    {
        ResolveMaterial();
        if (!runtimeMaterial) return;

        if (captureInitialRotationOnStart && runtimeMaterial.HasProperty(rotationPropertyName))
            currentAngle = runtimeMaterial.GetFloat(rotationPropertyName);
        else
        {
            currentAngle = initialAngleDegrees;
            if (runtimeMaterial.HasProperty(rotationPropertyName))
                runtimeMaterial.SetFloat(rotationPropertyName, currentAngle);
        }

        if (autoStartOnEnable) StartRotation();

        if (updateDynamicGI && giCoroutine == null && tryUpdateEnvironment != null)
            giCoroutine = StartCoroutine(DynamicGIUpdater());
    }

    private void OnDisable()
    {
        StopRotation();
        if (giCoroutine != null)
        {
            StopCoroutine(giCoroutine);
            giCoroutine = null;
        }
    }

    private void Update()
    {
        if (!isRunning || !runtimeMaterial) return;
        if (!runtimeMaterial.HasProperty(rotationPropertyName)) return;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        currentAngle += rotationSpeedDegreesPerSecond * dt;
        if (currentAngle > 360f || currentAngle < -360f) currentAngle %= 360f;

        runtimeMaterial.SetFloat(rotationPropertyName, currentAngle);
    }

    public void StartRotation()
    {
        if (!runtimeMaterial) ResolveMaterial();
        isRunning = true;
        Log("Rotation started");
    }

    public void StopRotation()
    {
        isRunning = false;
        Log("Rotation stopped");
    }

    public void SetRotationSpeed(float degreesPerSecond)
    {
        rotationSpeedDegreesPerSecond = degreesPerSecond;
        Log($"Speed set to {rotationSpeedDegreesPerSecond:0.##} dps");
    }

    public void SetAngle(float angleDegrees)
    {
        currentAngle = angleDegrees;
        if (runtimeMaterial && runtimeMaterial.HasProperty(rotationPropertyName))
            runtimeMaterial.SetFloat(rotationPropertyName, currentAngle);
        Log($"Angle set to {currentAngle:0.##}°");
    }

    private void ResolveMaterial()
    {
        if (runtimeMaterial) return;

        var src = targetSkyboxMaterial ? targetSkyboxMaterial : RenderSettings.skybox;
        if (!src)
        {
            Log("No skybox material found");
            return;
        }

        runtimeMaterial = instantiateSkyboxMaterial ? new Material(src) : src;

        if (!targetSkyboxMaterial && instantiateSkyboxMaterial)
            RenderSettings.skybox = runtimeMaterial;

        if (instantiateSkyboxMaterial && targetSkyboxMaterial)
            targetSkyboxMaterial = runtimeMaterial;

        Log($"Using {(instantiateSkyboxMaterial ? "instance of" : "shared")} material '{runtimeMaterial.name}'");
    }

    private IEnumerator DynamicGIUpdater()
    {
        float interval = Mathf.Max(0.02f, dynamicGIUpdateIntervalSeconds);

        if (useUnscaledTime)
        {
            var wait = new WaitForSecondsRealtime(interval);
            while (enabled && updateDynamicGI && tryUpdateEnvironment != null)
            {
                tryUpdateEnvironment();
                yield return wait;
            }
        }
        else
        {
            var wait = new WaitForSeconds(interval);
            while (enabled && updateDynamicGI && tryUpdateEnvironment != null)
            {
                tryUpdateEnvironment();
                yield return wait;
            }
        }

        giCoroutine = null;
    }


    private void Log(string msg)
    {
        if (!isDebugLoggingEnabled) return;
        Debug.Log($"[RotatingSkybox] {name}: {msg}", this);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.9f);
        var p = Camera.main ? Camera.main.transform.position : transform.position;
        Gizmos.DrawRay(p, Vector3.up * 0.75f);
    }
#endif
}
