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

    [SerializeField] private GameModeSO defaultModeToLoad;
    [SerializeField] private BaseLevelSO defaultLevelToLoad;
    private NullCheck<BaseLevelSO> _levelToLoad;
    private NullCheck<GameModeSO> _modeToLoad;
    private NullCheck<Volume> _globalVolume;
    private ActionObserver<GameModeSO, BaseLevelSO> _levelSelected;

    private bool _enabled;

    private void Awake()
    {
        _enabled = true;
        _globalVolume = FindObjectOfType<Volume>();
        _levelSelected = new ActionObserver<GameModeSO, BaseLevelSO>(SetTargetSession);
        LevelSelectorMediator.LevelSelected.Attach(_levelSelected);
    }

    private void Start()
    {
        LevelSelectorMediator.LevelSelected.NotifyAll(defaultModeToLoad, defaultLevelToLoad);
        if (_globalVolume)
        {
            _globalVolume.Get().profile = hubVolume;
        }
    }

    public void SetTargetSession(GameModeSO mode, BaseLevelSO level)
    {
        _levelToLoad = level;
        _modeToLoad = mode;
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

            if (_levelToLoad.TryGet(out var selectedLevel) && _modeToLoad.TryGet(out var selectedMode))
            {
                var session = GameSessionSO.GetOrCreate(GlobalLevelManager.CurrentSession, selectedMode, selectedLevel);
                GameEntry.LoadSessionAsync(session, false);
            }
            else
            {
                this.Log("The portal script has no level to go to...", LogType.Error);
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
