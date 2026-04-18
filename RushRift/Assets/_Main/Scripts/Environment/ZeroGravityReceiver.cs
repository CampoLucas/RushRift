using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ZeroGravityReceiver : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] bool debugLogs;

    readonly HashSet<object> sources = new();
    float moveMultiplier = 1f;

    public bool IsActive => sources.Count > 0;
    public float CurrentMoveMultiplier => moveMultiplier;

    public void EnterZeroG(object source, float moveMult = 0.25f)
    {
        if (source == null) return;
        sources.Add(source);
        moveMultiplier = Mathf.Clamp(moveMult, 0.05f, 1f);
    }

    public void ExitZeroG(object source)
    {
        if (source == null) return;
        sources.Remove(source);
        if (sources.Count == 0) moveMultiplier = 1f;
    }
}