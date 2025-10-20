using System;
using Game;
using UnityEngine;
using Game.DesignPatterns.Observers;
using UnityEngine.UI;
using TMPro;

public class TimerHandler : IDisposable
{
    public float CurrentTime { get; private set; }
    public void DoUpdate(float delta)
    {
        CurrentTime += delta;
        GlobalEvents.TimeUpdated.NotifyAll(delta);
    }

    public void Dispose()
    {
        GlobalEvents.TimeUpdated.DetachAll();
    }
}