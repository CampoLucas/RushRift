using System.Collections;
using System.Collections.Generic;
using Game;
using Game.DesignPatterns.Observers;
using Game.Levels;
using Game.UI;
using UnityEngine;

public class PowerSurgeFX : PauseBehaviour
{
    private static readonly int PowerUp = Shader.PropertyToID("PowerSurge");
    private static readonly int PowerUpPaused = Shader.PropertyToID("PowerSurgePaused");
    
    [SerializeField] private ParticleSystem particleSystem;

    private ActionObserver<BaseLevelSO> _onLevelLoaded;

    protected override void Start()
    {
        base.Start();

        _onLevelLoaded = new ActionObserver<BaseLevelSO>(OnLevelLoaded);
        GameEntry.LoadingState.AttachOnReady(_onLevelLoaded);
        particleSystem.Play();
    }

    protected override void OnPause()
    {
        particleSystem.Pause();
        Shader.SetGlobalFloat(PowerUpPaused , 1);
    }

    protected override void OnUnpause()
    {
        particleSystem.Play();
        Shader.SetGlobalFloat(PowerUpPaused, 0);
    }

    private void OnLevelLoaded(BaseLevelSO level)
    {
        Shader.SetGlobalFloat(PowerUp , GlobalLevelManager.PowerSurge ? 1 : 0);
    }

    protected override void OnDestroy()
    {
        GameEntry.LoadingState.DetachOnReady(_onLevelLoaded);
        _onLevelLoaded?.Dispose();
    }
}
