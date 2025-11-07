using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[AddComponentMenu("UI/System Time To TMP")]
public class SystemTimeToTMP : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("Text elements that will display the time.")]
    [SerializeField] private List<TMP_Text> _targetTexts = new List<TMP_Text>();

    [Header("Format")]
    [Tooltip("Use 24-hour time. If disabled, uses 12-hour time.")]
    [SerializeField] private bool _use24Hour = true;
    [Tooltip("Pad Hour/Minute/Second with leading zeros.")]
    [SerializeField] private bool _padWithZeros = true;
    [Tooltip("Separator used between Hour, Minute and Second.")]
    [SerializeField] private string _separator = ":";

    [Header("Update")]
    [Tooltip("Update every frame. If disabled, updates at a fixed interval.")]
    [SerializeField] private bool _updateEveryFrame = true;
    [Tooltip("Seconds between updates when not updating every frame.")]
    [SerializeField, Min(0.01f)] private float _updateInterval = 0.25f;

    [Header("Debug")]
    [Tooltip("Enable debug logs.")]
    [SerializeField] private bool _debugLogs = false;

    [Header("Gizmos")]
    [Tooltip("Draw gizmos in the Scene view.")]
    [SerializeField] private bool _drawGizmos = true;
    [Tooltip("Color of the gizmo.")]
    [SerializeField] private Color _gizmoColor = new Color(0.2f, 1f, 0.6f, 0.9f);
    [Tooltip("Radius of the gizmo sphere.")]
    [SerializeField, Min(0f)] private float _gizmoRadius = 0.1f;

    private float _timer;

    private void OnEnable()
    {
        UpdateAllTargets(BuildTimeString());
    }

    private void Update()
    {
        if (_updateEveryFrame)
        {
            UpdateAllTargets(BuildTimeString());
            return;
        }

        _timer += Time.unscaledDeltaTime;
        if (_timer >= _updateInterval)
        {
            _timer = 0f;
            UpdateAllTargets(BuildTimeString());
        }
    }

    public void ForceRefresh()
    {
        UpdateAllTargets(BuildTimeString());
    }

    private string BuildTimeString()
    {
        var now = DateTime.Now;
        int hour = _use24Hour ? now.Hour : ((now.Hour % 12 == 0) ? 12 : now.Hour % 12);
        int minute = now.Minute;
        int second = now.Second;

        if (_padWithZeros)
            return $"{hour:00}{_separator}{minute:00}{_separator}{second:00}";
        else
            return $"{hour}{_separator}{minute}{_separator}{second}";
    }

    private void UpdateAllTargets(string value)
    {
        for (int i = 0; i < _targetTexts.Count; i++)
        {
            var t = _targetTexts[i];
            if (!t) continue;
            if (t.text != value)
            {
                t.text = value;
                if (_debugLogs) Debug.Log($"[SystemTimeToTMP] Updated: {value}", this);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!_drawGizmos) return;
        Gizmos.color = _gizmoColor;
        Gizmos.DrawWireSphere(transform.position, _gizmoRadius);
    }
}
