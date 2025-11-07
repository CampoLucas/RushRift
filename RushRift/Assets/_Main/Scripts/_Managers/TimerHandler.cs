using System;
using Game;
using UnityEngine;
using Game.DesignPatterns.Observers;
using Game.Levels;
using UnityEngine.UI;
using TMPro;

public class TimerHandler : IDisposable, Game.DesignPatterns.Observers.IObserver<BaseLevelSO>, Game.DesignPatterns.Observers.IObserver<bool>
{
    public float CurrentTime { get; private set; }
    private bool _paused;

    public TimerHandler()
    {
        PauseHandler.Attach(this);
        _paused = PauseHandler.IsPaused;
    }

    public void DoUpdate(float delta)
    {
        if (_paused) return;
        
        CurrentTime += delta;
        GlobalEvents.TimeUpdated.NotifyAll(CurrentTime);
    }

    public void OnNotify(BaseLevelSO arg)
    {
        CurrentTime = 0;
    }
    
    public void OnNotify(bool paused)
    {
        _paused = paused;
    }

    public void Dispose()
    {
        PauseHandler.Detach(this);
        GlobalEvents.TimeUpdated.DetachAll();
    }
}