
using UnityEngine;

[DisallowMultipleComponent]
public class BillboardFXController : MonoBehaviour
{
    [Header("TARGET")]
    [SerializeField] private Renderer targetRenderer;

    [Header("HOLOGRAM COLOR")]
    [ColorUsage(true, true)]
    public Color hologramColor = new Color(0f, 0.85f, 1f, 1f);

    [Range(0f, 15f)]
    public float emissionStrength = 4f;

    [Range(0f, 1f)]
    public float opacity = 0.85f;

    [Header("ENERGY SEGMENTS")]
    [Range(4f, 64f)]
    public float segments = 24f;

    [Range(0.1f, 0.95f)]
    public float segmentWidth = 0.7f;

    [Range(-5f, 5f)]
    public float patternSpeed = 0.35f;

    [Header("SCANLINES")]
    [Range(2f, 100f)]
    public float scanlineDensity = 28f;

    [Range(-5f, 5f)]
    public float scanlineSpeed = 0.6f;

    [Header("PULSE")]
    public bool enablePulse = true;

    [Range(0f, 15f)]
    public float pulseSpeed = 2f;

    [Range(0f, 1f)]
    public float pulseAmount = 0.2f;

    [Header("GLITCH")]
    public bool enableGlitch = true;

    [Range(0f, 1f)]
    public float glitchAmount = 0.12f;

    [Range(1f, 30f)]
    public float glitchSpeed = 8f;

    [Header("PHYSICAL ROTATION")]
    public bool rotateTorus = false;

    public Vector3 rotationAxis = Vector3.up;

    public float rotationSpeed = 20f;

    [Header("WORLD OFFSET")]
    public bool applyWorldOffset = false;

    public Vector3 worldOffset = Vector3.zero;

    [Header("INTERACTION")]
    public bool enableInteractionPulse = true;

    public float interactionMultiplier = 2.5f;

    public float interactionDuration = 0.35f;

    // Shader property IDs
    private static readonly int ColorID =
        Shader.PropertyToID("_Color");

    private static readonly int IntensityID =
        Shader.PropertyToID("_Intensity");

    private static readonly int OpacityID =
        Shader.PropertyToID("_Opacity");

    private static readonly int SegmentsID =
        Shader.PropertyToID("_Segments");

    private static readonly int SegmentWidthID =
        Shader.PropertyToID("_SegmentWidth");

    private static readonly int RotationSpeedID =
        Shader.PropertyToID("_RotationSpeed");

    private static readonly int LineDensityID =
        Shader.PropertyToID("_LineDensity");

    private static readonly int ScanSpeedID =
        Shader.PropertyToID("_ScanSpeed");

    private static readonly int PulseSpeedID =
        Shader.PropertyToID("_PulseSpeed");

    private static readonly int GlitchAmountID =
        Shader.PropertyToID("_GlitchAmount");

    private static readonly int GlitchSpeedID =
        Shader.PropertyToID("_GlitchSpeed");

    // Internal
    private MaterialPropertyBlock propertyBlock;
    private Vector3 initialLocalPosition;

    private float interactionTimer;
    private bool interactionActive;

    private void Reset()
    {
        targetRenderer = GetComponent<Renderer>();
    }

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        if (targetRenderer == null)
        {
            Debug.LogError(
                "[RushRift Hologram] No Renderer found!",
                this
            );

            enabled = false;
            return;
        }

        propertyBlock = new MaterialPropertyBlock();

        initialLocalPosition = transform.localPosition;

        ApplyProperties();
    }

    private void Update()
    {
        UpdateRotation();
        UpdateWorldOffset();
        UpdateInteraction();
        ApplyProperties();
    }

    private void UpdateRotation()
    {
        if (!rotateTorus)
            return;

        if (rotationAxis.sqrMagnitude < 0.0001f)
            return;

        transform.Rotate(
            rotationAxis.normalized,
            rotationSpeed * Time.deltaTime,
            Space.Self
        );
    }

    private void UpdateWorldOffset()
    {
        if (applyWorldOffset)
        {
            transform.localPosition =
                initialLocalPosition + worldOffset;
        }
        else
        {
            transform.localPosition =
                initialLocalPosition;
        }
    }

    private void UpdateInteraction()
    {
        if (!interactionActive)
            return;

        interactionTimer -= Time.deltaTime;

        if (interactionTimer <= 0f)
        {
            interactionTimer = 0f;
            interactionActive = false;
        }
    }

    private void ApplyProperties()
    {
        if (targetRenderer == null)
            return;

        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();

        targetRenderer.GetPropertyBlock(propertyBlock);

        float finalEmission = emissionStrength;

        // Optional interaction boost
        if (interactionActive && enableInteractionPulse)
        {
            float normalizedTime = Mathf.Clamp01(
                interactionTimer /
                Mathf.Max(0.001f, interactionDuration)
            );

            float boost = Mathf.Lerp(
                1f,
                interactionMultiplier,
                normalizedTime
            );

            finalEmission *= boost;
        }

        propertyBlock.SetColor(
            ColorID,
            hologramColor
        );

        propertyBlock.SetFloat(
            IntensityID,
            finalEmission
        );

        propertyBlock.SetFloat(
            OpacityID,
            opacity
        );

        propertyBlock.SetFloat(
            SegmentsID,
            segments
        );

        propertyBlock.SetFloat(
            SegmentWidthID,
            segmentWidth
        );

        propertyBlock.SetFloat(
            RotationSpeedID,
            patternSpeed
        );

        propertyBlock.SetFloat(
            LineDensityID,
            scanlineDensity
        );

        propertyBlock.SetFloat(
            ScanSpeedID,
            scanlineSpeed
        );

        propertyBlock.SetFloat(
            PulseSpeedID,
            enablePulse ? pulseSpeed : 0f
        );

        propertyBlock.SetFloat(
            GlitchAmountID,
            enableGlitch ? glitchAmount : 0f
        );

        propertyBlock.SetFloat(
            GlitchSpeedID,
            glitchSpeed
        );

        targetRenderer.SetPropertyBlock(propertyBlock);
    }

    // =====================================
    // PUBLIC API
    // =====================================

    public void SetColor(Color newColor)
    {
        hologramColor = newColor;
        ApplyProperties();
    }

    public void SetEmission(float value)
    {
        emissionStrength = Mathf.Max(0f, value);
        ApplyProperties();
    }

    public void SetOpacity(float value)
    {
        opacity = Mathf.Clamp01(value);
        ApplyProperties();
    }

    public void SetPatternSpeed(float value)
    {
        patternSpeed = value;
        ApplyProperties();
    }

    public void SetGlitch(float amount)
    {
        enableGlitch = true;
        glitchAmount = Mathf.Clamp01(amount);
        ApplyProperties();
    }

    public void TriggerPulse()
    {
        if (!enableInteractionPulse)
            return;

        interactionTimer = Mathf.Max(
            0.001f,
            interactionDuration
        );

        interactionActive = true;

        ApplyProperties();
    }

    public void SetWorldOffset(Vector3 offset)
    {
        worldOffset = offset;
        applyWorldOffset = true;
    }

    public void ClearWorldOffset()
    {
        worldOffset = Vector3.zero;
        applyWorldOffset = false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        if (Application.isPlaying)
            ApplyProperties();
    }
#endif
}

