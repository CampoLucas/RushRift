using System;
using Game;
using Game.DesignPatterns.Observers;
using Game.Levels;
using Game.UI.Screens;
using Game.Utils;
using MyTools.Global;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Don't use this class, it is to prototype
/// </summary>
public class PortalPrototype : MonoBehaviour
{
    [SerializeField] private VolumeProfile hubVolume;
    [SerializeField] private VolumeProfile gameVolume;

    [SerializeField] private BaseLevelSO defaultLevelToLoad;
    private NullCheck<BaseLevelSO> _levelToLoad;
    private NullCheck<Volume> _globalVolume;
    private ActionObserver<BaseLevelSO> _levelSelected;

    private bool _enabled;

    private void Awake()
    {
        _enabled = true;
        _globalVolume = FindObjectOfType<Volume>();
        _levelSelected = new ActionObserver<BaseLevelSO>(SetTargetLevel);
        LevelSelectorMediator.LevelSelected.Attach(_levelSelected);
    }

    private void Start()
    {
        LevelSelectorMediator.LevelSelected.NotifyAll(defaultLevelToLoad);
        if (_globalVolume)
        {
            _globalVolume.Get().profile = hubVolume;
        }
    }

    public void SetTargetLevel(BaseLevelSO level)
    {
        _levelToLoad = level;
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

    private void OnDestroy()
    {
        LevelSelectorMediator.LevelSelected.Detach(_levelSelected);
        defaultLevelToLoad = null;
        hubVolume = null;
        gameVolume = null;
    }
}
