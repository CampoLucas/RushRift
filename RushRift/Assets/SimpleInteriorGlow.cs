
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class SimpleInteriorGlow : MonoBehaviour
{
    [Header("REFERENCES")]
    public ParticleSystem particleSystemInstance;
    public Light pointLightInstance;

    [Header("PARTICLE AREA")]
    public Vector3 boxSize = new Vector3(3f, 1.8f, 7f);
    public Vector3 particlesLocalOffset = new Vector3(0f, 1.2f, 0f);

    [Header("PARTICLES")]
    public Color particleColor = new Color(0f, 0.85f, 1f, 1f);

    [Range(0f, 50f)]
    public float emissionRate = 7f;

    [Range(0.005f, 0.2f)]
    public float startSize = 0.02f;

    [Range(0f, 1f)]
    public float startSpeed = 0.025f;

    [Range(1f, 20f)]
    public float lifetime = 7f;

    [Range(0f, 1f)]
    public float particleOpacity = 0.12f;

    [Header("MOVEMENT")]
    [Range(0f, 1f)]
    public float noiseStrength = 0.02f;

    [Header("AMBIENT LIGHT")]
    public bool enableLight = true;

    public Color lightColor = new Color(0f, 0.75f, 1f, 1f);

    [Range(0f, 5f)]
    public float lightIntensity = 0.65f;

    [Range(0.1f, 30f)]
    public float lightRange = 5.5f;

    public Vector3 lightLocalOffset = new Vector3(0f, 1.3f, 0f);

    private Material particleMaterial;

    private void Reset()
    {
        Setup();
    }

    private void OnEnable()
    {
        Setup();
    }

    private void OnValidate()
    {
        if (!isActiveAndEnabled)
            return;

        Setup();
    }

    private void Setup()
    {
        EnsureParticles();
        EnsureLight();

        ConfigureParticles();
        ConfigureLight();
    }

    private void EnsureParticles()
    {
        if (particleSystemInstance != null)
            return;

        Transform existing = transform.Find("NetworkDust_Particles");

        if (existing != null)
        {
            particleSystemInstance =
                existing.GetComponent<ParticleSystem>();

            if (particleSystemInstance != null)
                return;
        }

        GameObject go = new GameObject("NetworkDust_Particles");

        go.transform.SetParent(transform, false);

        particleSystemInstance =
            go.AddComponent<ParticleSystem>();
    }

    private void EnsureLight()
    {
        if (pointLightInstance != null)
            return;

        Transform existing = transform.Find("NetworkDust_Light");

        if (existing != null)
        {
            pointLightInstance = existing.GetComponent<Light>();

            if (pointLightInstance != null)
                return;
        }

        GameObject go = new GameObject("NetworkDust_Light");

        go.transform.SetParent(transform, false);

        pointLightInstance = go.AddComponent<Light>();
    }

    private void ConfigureParticles()
    {
        if (particleSystemInstance == null)
            return;

        ParticleSystem ps = particleSystemInstance;

        ps.transform.localPosition = particlesLocalOffset;
        ps.transform.localRotation = Quaternion.identity;
        ps.transform.localScale = Vector3.one;

        var main = ps.main;

        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace =
            ParticleSystemSimulationSpace.World;

        main.startLifetime = lifetime;
        main.startSpeed = startSpeed;
        main.startSize = startSize;
        main.startColor = Color.white;
        main.maxParticles = 300;
        main.gravityModifier = 0f;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = emissionRate;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = boxSize;

        var noise = ps.noise;
        noise.enabled = noiseStrength > 0f;
        noise.separateAxes = false;
        noise.strength = noiseStrength;
        noise.frequency = 0.25f;
        noise.scrollSpeed = 0.1f;
        noise.octaveCount = 1;

        // Smooth fade in/out
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;

        Gradient gradient = new Gradient();

        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(particleColor, 0f),
                new GradientColorKey(particleColor, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(particleOpacity, 0.2f),
                new GradientAlphaKey(particleOpacity, 0.8f),
                new GradientAlphaKey(0f, 1f)
            }
        );

        colorOverLifetime.color =
            new ParticleSystem.MinMaxGradient(gradient);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();

        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.sortMode = ParticleSystemSortMode.Distance;

        // Create material once, without overwriting existing materials
        if (renderer.sharedMaterial == null)
        {
            Shader shader = Shader.Find(
                "Universal Render Pipeline/Particles/Unlit"
            );

            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            if (shader != null)
            {
                particleMaterial = new Material(shader);
                particleMaterial.name = "M_NetworkDust";

                if (particleMaterial.HasProperty("_BaseColor"))
                    particleMaterial.SetColor(
                        "_BaseColor",
                        Color.white
                    );

                if (particleMaterial.HasProperty("_Color"))
                    particleMaterial.SetColor(
                        "_Color",
                        Color.white
                    );

                if (particleMaterial.HasProperty("_Surface"))
                    particleMaterial.SetFloat("_Surface", 1f);

                renderer.sharedMaterial = particleMaterial;
            }
        }

        if (Application.isPlaying && !ps.isPlaying)
            ps.Play();
    }

    private void ConfigureLight()
    {
        if (pointLightInstance == null)
            return;

        pointLightInstance.transform.localPosition =
            lightLocalOffset;

        pointLightInstance.type = LightType.Point;
        pointLightInstance.enabled = enableLight;
        pointLightInstance.color = lightColor;
        pointLightInstance.intensity = lightIntensity;
        pointLightInstance.range = lightRange;
        pointLightInstance.shadows = LightShadows.None;
    }
}
