using UnityEngine;

[ExecuteAlways]
public class NeonFireFlicker : MonoBehaviour
{
    [Header("Light")] 
    public Light pointLight;            // Si no la asignás, se crea una
    public Color lightColor = new Color(1.0f, 0.55f, 0.2f);
    public float baseIntensity = 2.8f;
    public float intensityAmp = 1.6f;
    public float baseRange = 5.5f;
    public float rangeAmp = 1.2f;
    public float speed = 2.2f;

    [Header("Material Emission (opcional)")]
    public Renderer emissionRenderer;   // quad del fuego (material con _EmissionColor)
    public Color emissionColor = new Color(1.0f, 0.45f, 0.1f);
    [Range(0f, 10f)] public float emissionMin = 2.5f;
    [Range(0f, 10f)] public float emissionMax = 6.0f;
    public float emissionSpeed = 2.0f;
    public float emissionNoiseMix = 0.35f;

    MaterialPropertyBlock _mpb;
    int _emissID;
    float _seed;

    void Reset() { emissionRenderer = GetComponentInChildren<Renderer>(); }

    void OnEnable()
    {
        _seed = Random.value * 100f;
        if (!pointLight)
        {
            var go = new GameObject("NeonFire_Light");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            pointLight = go.AddComponent<Light>();
            pointLight.type = LightType.Point;
        }
        pointLight.color = lightColor;

        _emissID = Shader.PropertyToID("_EmissionColor");
        if (_mpb == null) _mpb = new MaterialPropertyBlock();
        if (emissionRenderer != null)
        {
            foreach (var m in emissionRenderer.sharedMaterials) m?.EnableKeyword("_EMISSION");
        }
    }

    void Update()
    {
        float t = Time.time * speed;
        float n = Mathf.PerlinNoise(_seed, t);        // 0..1
        float k = (n - 0.5f) * 2f;                    // -1..1

        if (pointLight)
        {
            pointLight.intensity = baseIntensity + k * intensityAmp;
            pointLight.range = baseRange + k * rangeAmp;
            pointLight.color = lightColor;
        }

        if (emissionRenderer)
        {
            float s = Time.time * emissionSpeed;
            float sin = Mathf.Sin(s) * 0.5f + 0.5f;
            float n2 = Mathf.PerlinNoise(_seed + 7.2f, s * 0.85f);
            float mix = Mathf.Lerp(sin, n2, Mathf.Clamp01(emissionNoiseMix));
            float inten = Mathf.Lerp(emissionMin, emissionMax, mix);

            emissionRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(_emissID, emissionColor * inten);
            emissionRenderer.SetPropertyBlock(_mpb);
        }
    }
}
