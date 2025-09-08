using System;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
public sealed class PauseEventBus : MonoBehaviour
{
    public static event Action<bool> PauseChanged;

    public static bool IsPaused { get; private set; }

    [Header("Debug")]
    [SerializeField, Tooltip("If enabled, prints debug logs.")]
    private bool isDebugLoggingEnabled = false;

    public static void SetPaused(bool paused)
    {
        if (IsPaused == paused) return;
        IsPaused = paused;
        PauseChanged?.Invoke(paused);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        gameObject.name = nameof(PauseEventBus);
    }
#endif

    private void Awake()
    {
        if (FindObjectsOfType<PauseEventBus>().Length > 1)
        {
            if (isDebugLoggingEnabled) Debug.Log("[PauseEventBus] Duplicate found, destroying.", this);
            Destroy(gameObject);
        }
    }
}