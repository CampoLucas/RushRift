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
    [SerializeField] private TMP_Text timerText; // Gameplay

    [Header("Multiple UI Targets")] // Should be their own stand alone class
    [SerializeField] private TMP_Text[] timerTexts;
    [SerializeField] private TMP_Text[] finalTimerTexts;
    [SerializeField] private TMP_Text[] bestTimerTexts;

    [Header("Medal Icon")] // should be on the gameplay ui
    [SerializeField, Tooltip("Primary Image to display the current medal icon.")]
    private Image medalImage;
    [SerializeField, Tooltip("Optional additional medal images to keep in sync.")]
    private Image[] medalImages;
    [SerializeField, Tooltip("Sprite shown while within the Gold threshold.")]
    private Sprite goldSprite;
    [SerializeField, Tooltip("Sprite shown after exceeding Gold but within the Silver threshold.")]
    private Sprite silverSprite;
    [SerializeField, Tooltip("Sprite shown after exceeding Silver but within the Bronze threshold.")]
    private Sprite bronzeSprite;
    [SerializeField, Tooltip("Sprite shown after exceeding the Bronze threshold.")]
    private Sprite failSprite;
    [SerializeField, Tooltip("If true and medal data for this level is missing, hide the medal images.")]
    private bool hideIconIfNoMedalData = true;

    private float _timer;
    private bool _triggered;
    private bool stopTimer;
    private int[] _newTimer = new int[3];

    private IObserver _decreaseObserver;
    private IObserver _increaseObserver;
    private IObserver _onWinLevelObserver;

    private enum MedalState { Gold, Silver, Bronze, Fail, None }
    private MedalState _currentMedalState = MedalState.None;
    private bool _hasThresholds;
    private float _goldThreshold;
    private float _silverThreshold;
    private float _bronzeThreshold;

    private void Awake()
    {
        _onWinLevelObserver = new ActionObserver(OnWinLevel);

        WinTrigger.OnWinSaveTimes.Attach(_onWinLevelObserver);

        stopTimer = false;
        var data = SaveAndLoad.Load();

        ResolveMedalThresholds();
        InitializeMedalIconOnStart();
    }

    private void Update()
    {
        LevelTimer();
    }

    private void LevelTimer()
    {
        if (stopTimer) return;

        _timer += Time.deltaTime;
        _newTimer = TimerFormatter.GetNewTimer(_timer);
        FormatAll(timerText, timerTexts, _newTimer[0], _newTimer[1], _newTimer[2]);

        UpdateMedalIconForTime(_timer);
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

        // var best = TimerFormatter.GetNewTimer(data.BestTimes[currentLevel]);
        // FormatAll(bestTimerText, bestTimerTexts, best[0], best[1], best[2]);

        // var final = TimerFormatter.GetNewTimer(_timer);
        // FormatAll(finalTimerText, finalTimerTexts, final[0], final[1], final[2]);
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

    private void ResolveMedalThresholds()
    {
        _hasThresholds = false;
        _goldThreshold = _silverThreshold = _bronzeThreshold = float.PositiveInfinity;

        var list = LevelManager.GetMedals();
        if (list == null || list.Count == 0) { ApplyMedalVisibility(false); return; }

        for (int i = 0; i < list.Count; i++)
        {
            var m = list[i];
            if (m != null && m.levelNumber == currentLevel)
            {
                _goldThreshold = Mathf.Max(0f, m.levelMedalTimes.gold.time);
                _silverThreshold = Mathf.Max(0f, m.levelMedalTimes.silver.time);
                _bronzeThreshold = Mathf.Max(0f, m.levelMedalTimes.bronze.time);
                _hasThresholds = true;
                break;
            }
        }

        ApplyMedalVisibility(_hasThresholds || !hideIconIfNoMedalData);
    }

    private void InitializeMedalIconOnStart()
    {
        if (!_hasThresholds)
        {
            if (hideIconIfNoMedalData) ApplyMedalVisibility(false);
            else ApplyMedalState(MedalState.Fail);
            return;
        }

        // Start run at Gold, then degrade as time exceeds thresholds
        ApplyMedalState(MedalState.Gold);
    }

    private void UpdateMedalIconForTime(float time)
    {
        if (!_hasThresholds) return;

        MedalState next;
        if (time <= _goldThreshold) next = MedalState.Gold;
        else if (time <= _silverThreshold) next = MedalState.Silver;
        else if (time <= _bronzeThreshold) next = MedalState.Bronze;
        else next = MedalState.Fail;

        if (next != _currentMedalState) ApplyMedalState(next);
    }

    private void ApplyMedalState(MedalState state)
    {
        _currentMedalState = state;

        Sprite s =
            state == MedalState.Gold ? goldSprite :
            state == MedalState.Silver ? silverSprite :
            state == MedalState.Bronze ? bronzeSprite :
            failSprite;

        if (medalImage) medalImage.sprite = s;
        if (medalImages != null)
        {
            for (int i = 0; i < medalImages.Length; i++)
            {
                var img = medalImages[i];
                if (img) img.sprite = s;
            }
        }

        ApplyMedalVisibility(true);
    }

    private void ApplyMedalVisibility(bool visible)
    {
        if (medalImage) medalImage.enabled = visible;
        if (medalImages != null)
        {
            for (int i = 0; i < medalImages.Length; i++)
            {
                var img = medalImages[i];
                if (img) img.enabled = visible;
            }
        }
    }
}
