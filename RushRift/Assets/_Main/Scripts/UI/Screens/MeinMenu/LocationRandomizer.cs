using System.Collections;
using System.Collections.Generic;
using MyTools.Global;
using UnityEngine;
using Random = UnityEngine.Random;
using Game.ScreenEffects;

[AddComponentMenu("Locations/Location Randomizer")]
public class LocationRandomizer : MonoBehaviour
{
    [Header("Locations")]
    [SerializeField] private List<LocationData> locations;

    [Header("Switching")]
    [SerializeField] private bool autoSwitch = true;
    [SerializeField, Min(0.1f)] private float switchInterval = 10f;
    [SerializeField] private bool randomizeOnAwake = true;

    [Header("Fade (UI-based)")]
    [SerializeField] private FadeScreenUI fadeUI;

    [Header("Integration")]
    [Tooltip("Optional CameraWiggle to rebase after switching.")]
    [SerializeField] private CameraWiggle cameraWiggle;

    private int _currentIndex = -1;
    private Coroutine _loop;

    private void Awake()
    {
        if (locations == null)
        {
            this.Log("Locations are null.", LogType.Warning);
            return;
        }

        if (locations.Count == 0)
        {
            this.Log("Has 0 locations", LogType.Warning);
            return;
        }

        if (!cameraWiggle) cameraWiggle = GetComponent<CameraWiggle>();

        if (randomizeOnAwake)
        {
            _currentIndex = Random.Range(0, locations.Count);
            ApplyLocation(locations[_currentIndex]);
            if (cameraWiggle) cameraWiggle.RebaseFromCurrentTransform();
        }
        else
        {
            _currentIndex = -1;
        }
    }

    private void OnEnable()
    {
        if (autoSwitch && _loop == null)
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

    [ContextMenu("Save Location")]
    public void SaveLocation()
    {
        var tr = transform;
        locations.Add(new LocationData()
        {
            position = tr.position,
            rotation = tr.rotation,
            rotationEuler = tr.rotation.eulerAngles,
        });
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
        autoSwitch = true;
        if (_loop == null) _loop = StartCoroutine(AutoSwitchLoop());
    }

    public void StopAutoSwitching()
    {
        autoSwitch = false;
        if (_loop != null)
        {
            StopCoroutine(_loop);
            _loop = null;
        }
    }

    private IEnumerator AutoSwitchLoop()
    {
        var wait = new WaitForSeconds(switchInterval);
        while (autoSwitch && enabled)
        {
            yield return wait;
            yield return SwitchOnce();
        }
    }

    private IEnumerator SwitchOnce()
    {
        if (locations == null || locations.Count == 0)
            yield break;

        if (fadeUI != null)
            yield return fadeUI.FadeOutRoutine();

        int next = (_currentIndex < 0 || locations.Count == 1)
            ? Random.Range(0, locations.Count)
            : PickDifferentIndex(_currentIndex, locations.Count);

        _currentIndex = next;
        ApplyLocation(locations[_currentIndex]);
        if (cameraWiggle) cameraWiggle.RebaseFromCurrentTransform();

        if (fadeUI != null)
            yield return fadeUI.FadeInRoutine();
    }

    private static int PickDifferentIndex(int current, int count)
    {
        int idx = Random.Range(0, count - 1);
        if (idx >= current) idx++;
        return idx;
    }

    private void ApplyLocation(in LocationData loc)
    {
        var tr = transform;
        tr.position = loc.position;
        tr.rotation = loc.rotation;
    }
}

[System.Serializable]
public struct LocationData
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 rotationEuler;
}
