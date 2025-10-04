using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class LocationRandomizer : MonoBehaviour
{
    [SerializeField] private List<LocationData> locations;
    
    private void Awake()
    {
        if (locations == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("WARNING: Locations is null", this);
#endif
            return;
        }

        if (locations.Count == 0)
        {
#if UNITY_EDITOR
            Debug.LogWarning("WARNING: Locations count is 0", this);
#endif
            return;
        }

        var location = locations[Random.Range(0, locations.Count)];

        var tr = transform;
        
        tr.position = location.position;
        tr.rotation = location.rotation;
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
}

[System.Serializable]
public struct LocationData
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 rotationEuler;
}
