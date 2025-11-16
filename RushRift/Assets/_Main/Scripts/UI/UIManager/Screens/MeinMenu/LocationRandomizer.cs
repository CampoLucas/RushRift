using System.Collections;
using System.Collections.Generic;
using MyTools.Global;
using UnityEngine;
using Random = UnityEngine.Random;
using Game.ScreenEffects;

[AddComponentMenu("Locations/Location Randomizer")]
public class LocationRandomizer : MonoBehaviour
{
    [Header("Locations (Drag empty GameObjects here)")]
    [Tooltip("Ordered list of Transforms used as target locations.")]
    [SerializeField] private List<Transform> _locationPoints = new List<Transform>();
    [Tooltip("Optional parent under which new locations will be created when using SaveLocation.")]
    [SerializeField] private Transform _locationsParent;

    [Header("Switching")]
    [Tooltip("Automatically switch to a different location on a timer.")]
    [SerializeField] private bool _autoSwitch = true;
    [Tooltip("Seconds between switches.")]
    [SerializeField, Min(0.1f)] private float _switchInterval = 10f;
    [Tooltip("Pick a random location on Awake.")]
    [SerializeField] private bool _randomizeOnAwake = true;

    [Header("Fade (UI-based)")]
    [Tooltip("Optional fade UI used during switching.")]
    [SerializeField] private FadeScreenUI _fadeUI;

    [Header("Integration")]
    [Tooltip("Optional CameraWiggle to rebase after switching.")]
    [SerializeField] private CameraWiggle _cameraWiggle;

    [Header("Debug")]
    [Tooltip("Enable debug logs.")]
    [SerializeField] private bool _debugLogs = false;

    [Header("Gizmos")]
    [Tooltip("Draw gizmos for target locations.")]
    [SerializeField] private bool _drawGizmos = true;
    [Tooltip("Gizmo color for target points.")]
    [SerializeField] private Color _gizmoColor = new Color(0.2f, 0.8f, 1f, 0.9f);
    [Tooltip("Gizmo sphere radius at each target.")]
    [SerializeField, Min(0f)] private float _gizmoRadius = 0.15f;

    private int _currentIndex = -1;
    private Coroutine _loop;

    private void Awake()
    {
        if (_locationPoints == null)
        {
            this.Log("Location list is null.", LogType.Warning);
            return;
        }

        if (_locationPoints.Count == 0)
        {
            this.Log("No location points assigned.", LogType.Warning);
            return;
        }

        if (!_cameraWiggle) _cameraWiggle = GetComponent<CameraWiggle>();

        if (_randomizeOnAwake)
        {
            _currentIndex = Random.Range(0, _locationPoints.Count);
            ApplyLocation(_locationPoints[_currentIndex]);
            if (_cameraWiggle) _cameraWiggle.RebaseFromCurrentTransform();
            if (_debugLogs) Debug.Log($"[LocationRandomizer] Awake → Set to {_locationPoints[_currentIndex].name}");
        }
        else
        {
            _currentIndex = -1;
        }
    }

    private void OnEnable()
    {
        if (_autoSwitch && _loop == null)
            _loop = StartCoroutine(AutoSwitchLoop());
    }

    private void OnDisable()
    {
        if (_loop != null)
        {
            StopCoroutine(_loop);
            _loop = null;
        }
    }

    [ContextMenu("Save Location (Create Empty At Current Pose)")]
    public void SaveLocation()
    {
        var go = new GameObject($"Location_{_locationPoints.Count:00}");
        if (_locationsParent) go.transform.SetParent(_locationsParent, true);
        go.transform.position = transform.position;
        go.transform.rotation = transform.rotation;
        _locationPoints.Add(go.transform);
        if (_debugLogs) Debug.Log($"[LocationRandomizer] Saved new location: {go.name}");
    }

    [ContextMenu("Switch Now")]
    public void SwitchNow()
    {
        if (!isActiveAndEnabled) return;
        if (_loop != null) StopCoroutine(_loop);
        _loop = StartCoroutine(SwitchOnce());
    }

    public void StartAutoSwitching()
    {
        _autoSwitch = true;
        if (_loop == null) _loop = StartCoroutine(AutoSwitchLoop());
    }

    public void StopAutoSwitching()
    {
        _autoSwitch = false;
        if (_loop != null)
        {
            StopCoroutine(_loop);
            _loop = null;
        }
    }

    private IEnumerator AutoSwitchLoop()
    {
        var wait = new WaitForSeconds(_switchInterval);
        while (_autoSwitch && enabled)
        {
            yield return wait;
            yield return SwitchOnce();
        }
    }

    private IEnumerator SwitchOnce()
    {
        if (_locationPoints == null || _locationPoints.Count == 0)
            yield break;

        if (_fadeUI != null)
            yield return _fadeUI.FadeOutRoutine();

        int next = (_currentIndex < 0 || _locationPoints.Count == 1)
            ? Random.Range(0, _locationPoints.Count)
            : PickDifferentIndex(_currentIndex, _locationPoints.Count);

        _currentIndex = next;
        ApplyLocation(_locationPoints[_currentIndex]);
        if (_cameraWiggle) _cameraWiggle.RebaseFromCurrentTransform();
        if (_debugLogs) Debug.Log($"[LocationRandomizer] Switched → {_locationPoints[_currentIndex].name}");

        if (_fadeUI != null)
            yield return _fadeUI.FadeInRoutine();
    }

    private static int PickDifferentIndex(int current, int count)
    {
        int idx = Random.Range(0, count - 1);
        if (idx >= current) idx++;
        return idx;
    }

    private void ApplyLocation(Transform target)
    {
        if (!target) return;
        transform.position = target.position;
        transform.rotation = target.rotation;
    }

    private void OnDrawGizmos()
    {
        if (!_drawGizmos || _locationPoints == null) return;
        Gizmos.color = _gizmoColor;
        for (int i = 0; i < _locationPoints.Count; i++)
        {
            var t = _locationPoints[i];
            if (!t) continue;
            Gizmos.DrawWireSphere(t.position, _gizmoRadius);
            Gizmos.DrawLine(t.position, t.position + t.forward * (_gizmoRadius * 2f));
        }
    }
}