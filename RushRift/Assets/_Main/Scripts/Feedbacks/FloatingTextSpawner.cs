using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class FloatingTextSpawner : MonoBehaviour
{
    [Header("Prefab & Pool")]
    [SerializeField, Tooltip("FloatingText prefab to spawn.")]
    private FloatingText floatingTextPrefab;
    [SerializeField, Tooltip("Initial pool size.")]
    private int initialPoolSize = 16;
    [SerializeField, Tooltip("Allow the pool to expand when exhausted.")]
    private bool allowPoolExpansion = true;

    [Header("Defaults")]
    [SerializeField, Tooltip("Default lifetime if the caller doesn't provide one.")]
    private float defaultLifetimeSeconds = 0.9f;
    [SerializeField, Tooltip("Default direction if the caller doesn't provide one.")]
    private Vector3 defaultDirection = Vector3.up;
    [SerializeField, Tooltip("Use unscaled time by default.")]
    private bool defaultUseUnscaledTime = true;

    [Header("Global Access")]
    [SerializeField, Tooltip("Register this spawner as a global singleton for static Spawn calls.")]
    private bool registerAsGlobalInstance = true;

    [Header("Debug")]
    [SerializeField, Tooltip("If enabled, prints detailed logs.")]
    private bool isDebugLoggingEnabled = false;
    [SerializeField, Tooltip("Draw gizmos for pool status.")]
    private bool drawGizmos = true;
    [SerializeField, Tooltip("Gizmo color for idle pool items.")]
    private Color gizmoIdleColor = new Color(0.2f, 1f, 0.6f, 0.85f);
    [SerializeField, Tooltip("Gizmo color for active items.")]
    private Color gizmoActiveColor = new Color(1f, 0.8f, 0.2f, 0.85f);

    private readonly List<FloatingText> pool = new List<FloatingText>(64);
    private static FloatingTextSpawner s_instance;

    /// <summary>Returns the current global spawner instance, if any.</summary>
    public static FloatingTextSpawner Instance => s_instance;

    /// <summary>Initializes the pool and registers as global if requested.</summary>
    private void Awake()
    {
        if (registerAsGlobalInstance) s_instance = this;
        BuildPool();
    }

    /// <summary>Clears the global instance if it points to this spawner.</summary>
    private void OnDestroy()
    {
        if (s_instance == this) s_instance = null;
    }

    /// <summary>Spawns a floating text at the specified world position.</summary>
    public FloatingText Spawn(string text, Vector3 worldPosition, Vector3? direction = null, float intensity = 1f, float? lifetime = null, Gradient colorGradient = null, Transform attachment = null, bool? useUnscaledTime = null)
    {
        if (!floatingTextPrefab) { Log("Spawn ignored: missing prefab"); return null; }
        var ft = GetFromPool();
        var life = Mathf.Max(0.01f, lifetime ?? defaultLifetimeSeconds);
        var dir = direction ?? defaultDirection;
        var unscaled = useUnscaledTime ?? defaultUseUnscaledTime;

        ft.gameObject.SetActive(true);
        ft.SetUseUnscaledTime(unscaled);
        ft.Play(text, worldPosition, dir, Mathf.Max(0f, intensity), life, colorGradient, attachment, OnDespawn);
        return ft;
    }

    /// <summary>Static helper to spawn using the global spawner instance.</summary>
    public static FloatingText SpawnGlobal(string text, Vector3 worldPosition, Vector3? direction = null, float intensity = 1f, float? lifetime = null, Gradient colorGradient = null, Transform attachment = null, bool? useUnscaledTime = null)
    {
        if (!s_instance) return null;
        return s_instance.Spawn(text, worldPosition, direction, intensity, lifetime, colorGradient, attachment, useUnscaledTime);
    }

    /// <summary>Pre-fills the pool with inactive instances.</summary>
    private void BuildPool()
    {
        pool.Clear();
        int count = Mathf.Max(0, initialPoolSize);
        for (int i = 0; i < count; i++) CreatePooled();
        Log($"Pool built: {pool.Count}");
    }

    /// <summary>Returns an available instance from the pool, expanding if necessary.</summary>
    private FloatingText GetFromPool()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (!pool[i].gameObject.activeSelf) return pool[i];
        }

        if (!allowPoolExpansion) return pool[0];
        return CreatePooled();
    }

    /// <summary>Creates and registers a new pooled instance.</summary>
    private FloatingText CreatePooled()
    {
        var go = Instantiate(floatingTextPrefab, transform);
        go.gameObject.SetActive(false);
        pool.Add(go);
        return go;
    }

    /// <summary>Handles despawn callback from FloatingText instances.</summary>
    private void OnDespawn(FloatingText ft)
    {
        if (!ft) return;
        ft.gameObject.SetActive(false);
    }

    /// <summary>Writes a debug message if logging is enabled.</summary>
    private void Log(string msg)
    {
        if (!isDebugLoggingEnabled) return;
        Debug.Log($"[FloatingTextSpawner] {name}: {msg}", this);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        int active = 0;
        for (int i = 0; i < pool.Count; i++) if (pool[i] && pool[i].gameObject.activeSelf) active++;
        float k = pool.Count > 0 ? (float)active / pool.Count : 0f;

        Gizmos.color = Color.Lerp(gizmoIdleColor, gizmoActiveColor, k);
        Gizmos.DrawWireSphere(transform.position, 0.25f);
        Gizmos.DrawRay(transform.position, Vector3.up * Mathf.Lerp(0.1f, 0.4f, k));
    }
#endif
}
