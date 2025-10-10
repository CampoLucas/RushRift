using System;
using System.Collections;
using System.Collections.Generic;
using Game;
using Game.Levels;
using Game.Utils;
using MyTools.Global;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

/// <summary>
/// Don't use this class, it is to prototype
/// </summary>
public class PortalPrototype : MonoBehaviour
{
    [SerializeField] private int buildIndex;

    [FormerlySerializedAs("HubVolume")] [SerializeField] private VolumeProfile hubVolume;
    [FormerlySerializedAs("GameVolume")] [SerializeField] private VolumeProfile gameVolume;

    [SerializeField] private BaseLevelSO defaultLevelToLoad;
    private NullCheck<BaseLevelSO> _levelToLoad;
    private NullCheck<Volume> _globalVolume;

    private bool _enabled;

    private void Awake()
    {
        _enabled = true;
        _globalVolume = FindObjectOfType<Volume>();
    }

    private void Start()
    {
        if (_globalVolume)
        {
            _globalVolume.Get().profile = hubVolume;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_enabled) return;
        
        
        if (other.gameObject.layer == 8)
        {
            _enabled = false;
            
            if (_globalVolume)
            {
                _globalVolume.Get().profile = gameVolume;
            }

            var targetLevel = _levelToLoad ? _levelToLoad.Get() : defaultLevelToLoad;

            if (targetLevel)
            {
                targetLevel.LoadLevel();
            }
            else
            {
                this.Log("The portal script has no level to go to, automatically going to the first level.", LogType.Warning);
                SceneHandler.LoadFirstLevel();
            }
            //SceneHandler.LoadFirstLevel();
        }
    }
}
