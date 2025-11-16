using UnityEngine;

[DisallowMultipleComponent]
public class BillboardFXController : MonoBehaviour
{
    [Header("Target")]
    public Renderer targetRenderer;
    public bool useSpriteRenderer = false;

    [Header("Base (arte del cartel)")]
    public Texture baseTexture;
    public Color baseTint = Color.white;
    [Range(0f, 50f)] public float emissionStrength = 10f;

    [Tooltip("Scroll del UV (px/seg en unidades UV).")]
    public Vector2 scrollSpeed = new Vector2(0.08f, 0f);

    [Header("UV Offset & Tiling")]
    [Tooltip("Offset manual en UV (se suma al scroll).")]
    public Vector2 uvOffset = Vector2.zero;
    [Tooltip("Escala UV (1,1 por defecto).")]
    public Vector2 uvTiling = Vector2.one;

    [Header("Flicker")]
    [Range(0f, 40f)] public float flickerSpeed = 8f;
    [Range(0f, 1f)] public float flickerAmount = 0.2f;

    [Header("Glitch (sacudidas UV)")]
    [Range(0f, 0.2f)] public float glitchMaxOffsetX = 0.02f;
    [Range(0f, 30f)] public float glitchBurstPerMin = 12f;
    [Range(0.01f, 0.5f)] public float glitchBurstDuration = 0.12f;

    [Header("Scanlines (overlay aditivo)")]
    public bool enableScanlines = true;
    [Range(64, 4096)] public int scanlineDensity = 1200;
    [Range(0f, 1f)] public float scanlineContrast = 0.5f;
    public float scanlineScrollSpeed = 2.0f;
    public Renderer scanlineRenderer;

    [Header("World Offset (opcional)")]
    public bool applyWorldOffset = false;
    public Vector3 worldOffset = Vector3.zero;

    // Internos
    MaterialPropertyBlock _mpb;
    int _baseMapId, _baseColorId;
    Vector2 _uvScrollAccum;
    bool _glitchActive;
    float _glitchUntil;
    Texture2D _scanlineTex;
    Vector3 _initialLocalPos;

    void Reset()
    {
        targetRenderer = GetComponent<Renderer>();
        useSpriteRenderer = (GetComponent<SpriteRenderer>() != null);
    }

    void Awake()
    {
        _initialLocalPos = transform.localPosition;

        if (!targetRenderer) targetRenderer = GetComponent<Renderer>();
        if (!targetRenderer)
        {
            Debug.LogError("[BillboardFX] No hay Renderer. Agregá MeshRenderer o SpriteRenderer.");
            enabled = false; return;
        }

        _mpb = new MaterialPropertyBlock();
        _baseMapId = Shader.PropertyToID("_BaseMap");     // URP/Unlit
        _baseColorId = Shader.PropertyToID("_BaseColor");   // URP/Unlit

        if (enableScanlines)
        {
            EnsureScanlineRenderer();
            BuildScanlineTexture();
            ApplyScanlineTexture();
        }

        if (baseTexture) SetBaseTexture(baseTexture);
    }

    void Update()
    {
        float t = Time.time;

        // --- World Offset opcional ---
        if (applyWorldOffset)
            transform.localPosition = _initialLocalPos + worldOffset;
        else
            transform.localPosition = _initialLocalPos;

        // --- Scroll acumulado ---
        _uvScrollAccum += scrollSpeed * Time.deltaTime;

        // --- Flicker ---
        float flicker = 1f + (Mathf.Sin(t * flickerSpeed) * 0.5f + 0.5f) * flickerAmount;

        // --- Glitch bursts ---
        if (!_glitchActive)
        {
            float p = glitchBurstPerMin / 60f * Time.deltaTime;
            if (Random.value < p) { _glitchActive = true; _glitchUntil = t + glitchBurstDuration; }
        }
        float glitchOffsetX = 0f;
        if (_glitchActive)
        {
            glitchOffsetX = Random.Range(-glitchMaxOffsetX, glitchMaxOffsetX);
            if (t >= _glitchUntil) _glitchActive = false;
        }

        // --- MPB para material base ---
        targetRenderer.GetPropertyBlock(_mpb);

        // _BaseMap_ST = (scaleX, scaleY, offsetX, offsetY)
        // Tiling y offset combinando: uvTiling + (uvOffset + scroll + glitchX)
        Vector4 st = _mpb.GetVector("_BaseMap_ST");
        if (st == Vector4.zero) st = new Vector4(1, 1, 0, 0);
        st.x = Mathf.Approximately(uvTiling.x, 0f) ? 1f : uvTiling.x;
        st.y = Mathf.Approximately(uvTiling.y, 0f) ? 1f : uvTiling.y;
        st.z = uvOffset.x + _uvScrollAccum.x + glitchOffsetX; // offset X
        st.w = uvOffset.y + _uvScrollAccum.y;                 // offset Y
        _mpb.SetVector("_BaseMap_ST", st);

        // Color “emisión” simulado
        Color baseCol = baseTint * EmissionToColor(emissionStrength * flicker);
        _mpb.SetColor(_baseColorId, baseCol);

        if (baseTexture) _mpb.SetTexture(_baseMapId, baseTexture);
        targetRenderer.SetPropertyBlock(_mpb);

        // --- Overlay scanlines ---
        if (enableScanlines && scanlineRenderer)
        {
            var mpb2 = new MaterialPropertyBlock();
            scanlineRenderer.GetPropertyBlock(mpb2);

            Vector4 st2 = mpb2.GetVector("_BaseMap_ST");
            if (st2 == Vector4.zero) st2 = new Vector4(1, 1, 0, 0);
            st2.w += scanlineScrollSpeed * Time.deltaTime; // mover líneas
            mpb2.SetVector("_BaseMap_ST", st2);

            float intensity = Mathf.Lerp(0.25f, 1.25f, scanlineContrast);
            mpb2.SetColor("_BaseColor", new Color(intensity, intensity, intensity, 1));
            mpb2.SetTexture("_BaseMap", _scanlineTex);

            scanlineRenderer.SetPropertyBlock(mpb2);
        }
    }

    // ----------------- API pública -----------------
    public void SetUVOffset(Vector2 uv) => uvOffset = uv;
    public void NudgeUV(Vector2 delta) => uvOffset += delta;
    public void SetUVTiling(Vector2 tiling) => uvTiling = tiling;
    public void SetWorldOffset(Vector3 wofs) { applyWorldOffset = true; worldOffset = wofs; }
    public void ClearWorldOffset() { applyWorldOffset = false; worldOffset = Vector3.zero; }

    // ----------------- Helpers internos -----------------
    Material GetSharedMaterial(Renderer r)
    {
        if (r is SpriteRenderer sr) return sr.sharedMaterial;
        var mats = r.sharedMaterials;
        return (mats != null && mats.Length > 0) ? mats[0] : null;
    }

    void SetBaseTexture(Texture tex)
    {
        if (targetRenderer is SpriteRenderer sr)
        {
            sr.sharedMaterial.SetTexture(_baseMapId, tex);
        }
        else
        {
            var mats = targetRenderer.sharedMaterials;
            if (mats != null && mats.Length > 0) mats[0].SetTexture(_baseMapId, tex);
        }
    }

    void EnsureScanlineRenderer()
    {
        if (scanlineRenderer != null) return;

        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "[ScanlinesOverlay]";
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.forward * 0.0001f;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        var r = go.GetComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        r.sharedMaterial = mat;
        scanlineRenderer = r;
    }

    void BuildScanlineTexture()
    {
        if (!enableScanlines) return;

        int height = Mathf.Clamp(scanlineDensity / 4, 64, 4096);
        int width = 256;

        if (_scanlineTex != null) Destroy(_scanlineTex);
        _scanlineTex = new Texture2D(width, height, TextureFormat.R8, true, false)
        {
            name = "ScanlinesProcedural",
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear
        };

        var cols = new Color32[width * height];
        byte dark = (byte)Mathf.RoundToInt(Mathf.Lerp(10, 60, scanlineContrast));
        byte bright = (byte)Mathf.RoundToInt(Mathf.Lerp(120, 255, scanlineContrast));

        for (int y = 0; y < height; y++)
        {
            byte v = (y % 2 == 0) ? bright : dark;
            for (int x = 0; x < width; x++) cols[y * width + x] = new Color32(v, v, v, 255);
        }
        _scanlineTex.SetPixels32(cols);
        _scanlineTex.Apply(true, false);
    }

    void ApplyScanlineTexture()
    {
        if (!scanlineRenderer || _scanlineTex == null) return;

        var mat = scanlineRenderer.sharedMaterial;
        if (!mat || mat.shader == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            scanlineRenderer.sharedMaterial = mat;
        }
        mat.SetTexture("_BaseMap", _scanlineTex);
        mat.SetColor("_BaseColor", Color.white);
        mat.SetVector("_BaseMap_ST", new Vector4(1f, 1f, 0f, 0f));
    }

    Color EmissionToColor(float e)
    {
        float m = Mathf.Lerp(1f, 4f, Mathf.Clamp01(e / 50f));
        return new Color(m, m, m, 1f);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (enableScanlines)
        {
            if (Application.isPlaying)
            {
                BuildScanlineTexture();
                ApplyScanlineTexture();
            }
        }
    }
#endif
}
