using System;
using Game;
using UnityEngine;
using Game.DesignPatterns.Observers;
using UnityEngine.UI;
using TMPro;

public class TimerHandler : IDisposable
{
    public float CurrentTime { get; private set; }
    public ISubject<float> OnTimeUpdated { get; private set; } = new Subject<float>();

    public void DoUpdate(float delta)
    {
        CurrentTime += delta;
        OnTimeUpdated.NotifyAll(CurrentTime);
    }

    public void Dispose()
    {
        OnTimeUpdated.DetachAll();
        OnTimeUpdated.Dispose();
        OnTimeUpdated = null;
    }
}