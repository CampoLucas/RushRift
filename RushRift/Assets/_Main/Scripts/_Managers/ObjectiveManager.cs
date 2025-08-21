using System;
using Game;
using UnityEngine;
using Game.DesignPatterns.Observers;
using UnityEngine.UI;
using TMPro;
using Game.Entities;
using UnityEngine.SceneManagement;

public class ObjectiveManager : MonoBehaviour
{
    public int currentLevel => SceneManager.GetActiveScene().buildIndex;

    [Header("Single UI Targets (optional, kept for backward-compat)")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text finalTimerText;
    [SerializeField] private TMP_Text bestTimerText;
    [SerializeField] private TMP_Text currentEnemiesText;

    [Header("Multiple UI Targets")]
    [SerializeField] private TMP_Text[] timerTexts;
    [SerializeField] private TMP_Text[] finalTimerTexts;
    [SerializeField] private TMP_Text[] bestTimerTexts;

    private float _timer;
    private bool _triggered;
    private bool stopTimer;
    private int _currentEnemies = 0;
    private int[] _newTimer = new int[3];

    private IObserver _decreaseObserver;
    private IObserver _increaseObserver;
    private IObserver _onWinLevelObserver;

    private void Awake()
    {
        _decreaseObserver = new ActionObserver(DecreseEnemyQuantity);
        _increaseObserver = new ActionObserver(EnemyQuantity);
        _onWinLevelObserver = new ActionObserver(OnWinLevel);

        WinTrigger.OnWinSaveTimes.Attach(_onWinLevelObserver);
        LevelManager.OnEnemyDeathSubject.Attach(_decreaseObserver);
        LevelManager.OnEnemySpawnSubject.Attach(_increaseObserver);

        stopTimer = false;
        var data = SaveAndLoad.Load();
    }

    private void Update()
    {
        LevelTimer();
    }

    private void EnemyQuantity()
    {
        _currentEnemies++;
        if (currentEnemiesText) currentEnemiesText.text = _currentEnemies.ToString();
    }

    private void DecreseEnemyQuantity()
    {
        _currentEnemies--;
        if (currentEnemiesText) currentEnemiesText.text = _currentEnemies.ToString();
    }

    private void LevelTimer()
    {
        if (stopTimer) return;

        _timer += Time.deltaTime;
        _newTimer = TimerFormatter.GetNewTimer(_timer);
        FormatAll(timerText, timerTexts, _newTimer[0], _newTimer[1], _newTimer[2]);
    }

    private void OnWinLevel()
    {
        if (_triggered) return;
        _triggered = true;

        stopTimer = true;

        LevelManager.SetLevelCompleteTime(_timer);

        var data = SaveAndLoad.Load();

        if (!data.BestTimes.ContainsKey(currentLevel)) data.BestTimes.Add(currentLevel, _timer);
        if (data.BestTimes[currentLevel] > _timer) data.BestTimes[currentLevel] = _timer;

        SaveAndLoad.Save(data);

        var best = TimerFormatter.GetNewTimer(data.BestTimes[currentLevel]);
        FormatAll(bestTimerText, bestTimerTexts, best[0], best[1], best[2]);

        var final = TimerFormatter.GetNewTimer(_timer);
        FormatAll(finalTimerText, finalTimerTexts, final[0], final[1], final[2]);
    }

    private void OnDestroy()
    {
        WinTrigger.OnWinSaveTimes.Detach(_onWinLevelObserver);
        LevelManager.OnEnemyDeathSubject.Detach(_decreaseObserver);
        LevelManager.OnEnemySpawnSubject.Detach(_increaseObserver);

        _onWinLevelObserver?.Dispose();
        _decreaseObserver?.Dispose();
        _increaseObserver?.Dispose();
    }

    private static void FormatAll(TMP_Text single, TMP_Text[] many, int minutes, int seconds, int milliseconds)
    {
        if (single) TimerFormatter.FormatTimer(single, minutes, seconds, milliseconds);
        if (many == null) return;
        for (int i = 0; i < many.Length; i++)
        {
            var t = many[i];
            if (t) TimerFormatter.FormatTimer(t, minutes, seconds, milliseconds);
        }
    }
}
