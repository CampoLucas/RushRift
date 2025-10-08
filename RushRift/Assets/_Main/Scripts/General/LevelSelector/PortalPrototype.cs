using System;
using System.Collections;
using System.Collections.Generic;
using Game;
using Game.Utils;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Don't use this class, it is to prototype
/// </summary>
public class PortalPrototype : MonoBehaviour
{
    [SerializeField] private int buildIndex;

    [SerializeField] private VolumeProfile HubVolume;
    [SerializeField] private VolumeProfile GameVolume;

    private NullCheck<Volume> globalVolume;

    private bool _enabled;

    private void Awake()
    {
        _enabled = true;
        globalVolume = FindObjectOfType<Volume>();
    }

    private void Start()
    {
        if (globalVolume)
        {
            globalVolume.Get().profile = HubVolume;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_enabled) return;
        
        
        if (other.gameObject.layer == 8)
        {
            _enabled = false;
            
            if (globalVolume)
            {
                globalVolume.Get().profile = GameVolume;
            }
            
            SceneHandler.LoadFirstLevel();
        }
    }
}
