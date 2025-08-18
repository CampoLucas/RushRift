using System;
using UnityEngine;
using Game.DesignPatterns.Observers;
using Game;
using Game.Entities;
using TMPro;



public class ScoreManager : MonoBehaviour
{
    public int CurrentPoints => currentPoints;
    
    [SerializeField] private TMP_Text scoreText;
    private int currentPoints;
    private int playerCurrency;
    private bool _triggered;
    private Game.DesignPatterns.Observers.IObserver<int> _onPointsGainObserver;
    private Game.DesignPatterns.Observers.IObserver<int> _onWinLevelObserver;

    private void Start()
    {
        _onPointsGainObserver = new ActionObserver<int>(OnPointsGain);
        _onWinLevelObserver = new ActionObserver<int>(OnWinLevel);

        LevelManager.OnEnemyGivesPoints.Attach(_onPointsGainObserver);
        WinTrigger.OnWinGivePoints.Attach(_onWinLevelObserver);
        scoreText.text = currentPoints.ToString();
    }


    public void OnPointsGain(int points)
    {
        currentPoints += points;
        playerCurrency += currentPoints;
        scoreText.text = currentPoints.ToString();
    }

    public void OnWinLevel(int points)
    {
        if (_triggered) return;
        _triggered = true;
        var data = SaveAndLoad.Load();
        OnPointsGain(points);
        data.playerCurrency += playerCurrency;
        SaveAndLoad.Save(data);

    }

    private void OnDestroy()
    {
        LevelManager.OnEnemyGivesPoints.Detach(_onPointsGainObserver);
        WinTrigger.OnWinGivePoints.Detach(_onWinLevelObserver);
        
        _onPointsGainObserver?.Dispose();
        _onPointsGainObserver = null;
        
        _onWinLevelObserver?.Dispose();
        _onWinLevelObserver = null;

        scoreText = null;
    }
}
