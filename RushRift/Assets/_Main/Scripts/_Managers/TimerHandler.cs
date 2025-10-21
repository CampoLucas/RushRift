using System;
using Game;
using UnityEngine;
using Game.DesignPatterns.Observers;
using Game.Levels;
using UnityEngine.UI;
using TMPro;

public class TimerHandler : IDisposable, Game.DesignPatterns.Observers.IObserver<BaseLevelSO>
{
    public float CurrentTime { get; private set; }
    
    public void DoUpdate(float delta)
    {
        CurrentTime += delta;
        GlobalEvents.TimeUpdated.NotifyAll(CurrentTime);
    }

    public void OnNotify(BaseLevelSO arg)
    {
        CurrentTime = 0;
    }

    public void Dispose()
    {
        GlobalEvents.TimeUpdated.DetachAll();
    }
}