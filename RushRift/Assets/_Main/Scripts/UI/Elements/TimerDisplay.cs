using System;
using System.Collections;
using System.Collections.Generic;
using Game;
using Game.DesignPatterns.Observers;
using Game.Levels;
using MyTools.Global;
using MyTools.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TimerDisplay : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_Text text;

    [Header("Icon")]
    [SerializeField] private Graphic icon;

    [Header("Colors")]
    [SerializeField] private SerializedDictionary<MedalType, Color> medalColors = new();
    [SerializeField] private Color failureColor;

    [Header("Settings")]
    [SerializeField] private bool useSameColor; // Makes the text have the same color as the medals.
    [SerializeField] private bool useNextThresholdColor = true; // Use the next threshold color instead of the blinkColor when blinking.
    [SerializeField] private bool useFailureTextColor = true; // Makes the text use failure color when passing the failure threshold.
    [SerializeField] private Color blinkColor;
    [SerializeField] private float blinkInterval = 0.5f;
    [SerializeField] private float timeToBlink = 5;
    [SerializeField] private float colorLerpSpeed = 10f;

    private readonly List<MedalData> _medals = new();
    
    private Color _textStartColor;
    private Color _currentTargetColor;
    private Color _previousTargetColor;
    private Color _nextThresholdColor = Color.white; // for blinking
    private Coroutine _blinkCoroutine;
    private bool _useBlinkColor;

    private ActionObserver<BaseLevelSO> _onPreload;
    private ActionObserver<BaseLevelSO> _onReady;
    private ActionObserver<float> _timerObserver;

    private struct MedalData
    {
        public float Time;
        public Color Color;
    }

    private void Awake()
    {
        _timerObserver ??= new ActionObserver<float>(OnTimeUpdated);
        _onPreload = new ActionObserver<BaseLevelSO>(OnLoadingStartHandler);
        _onReady = new ActionObserver<BaseLevelSO>(OnReadyHandler);
        _textStartColor = text.color;

        GameEntry.LoadingState.AttachOnPreload(_onPreload);
        GameEntry.LoadingState.AttachOnReady(_onReady);
        
        
    }

    private void Start()
    {
        if (GlobalLevelManager.CurrentLevel.TryGet(out var level))
        {
            _onReady.OnNotify(level);
            
        }
    }

    private void OnLoadingStartHandler(BaseLevelSO level)
    {
        StopAllCoroutines();
        if (_timerObserver != null)
        {
            GlobalEvents.TimeUpdated.Detach(_timerObserver);
        }
    }
    
    private void OnReadyHandler(BaseLevelSO level)
    {
        StopAllCoroutines();
        _timerObserver ??= new ActionObserver<float>(OnTimeUpdated);
        GlobalEvents.TimeUpdated.Attach(_timerObserver);
        
        _medals.Clear();
        _blinkCoroutine = null;
        _useBlinkColor = false;

        if (!level)
        {
            this.Log("TimerDisplay couldn't find the level config", LogType.Error);
            gameObject.SetActive(false);
            return;
        }

        if (!level.UsesMedals)
        {
            gameObject.SetActive(false);
            return;
        }

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }


        var medalTypes = level.GetMedalTypes();
        for (var i = 0; i < medalTypes.Length; i++)
        {
            TryAddMedal(level, medalTypes[i]);
        }
        
        // Sort by time ascending
        _medals.Sort((a, b) => a.Time.CompareTo(b.Time));
        
        if (_medals.Count > 0)
        {
            _currentTargetColor = _previousTargetColor = _medals[0].Color;

            // initialize next threshold color properly
            _nextThresholdColor = (_medals.Count > 1)
                ? _medals[1].Color
                : failureColor;
        }
        else
        {
            _currentTargetColor = _previousTargetColor = failureColor;
            _nextThresholdColor = failureColor;
        }
    }
    
    private void TryAddMedal(BaseLevelSO level, MedalType type)
    {
        var medal = level.GetMedal(type);
        if (medal.requiredTime > 0)
        {
            if (!medalColors.TryGetValue(type, out var color))
            {
                color = Color.magenta; // Magenta so it is clearly noticeable as an error
            }
            _medals.Add(new MedalData { Time = Mathf.Max(0f, medal.requiredTime), Color = color });
        }
    }

    private void OnTimeUpdated(float time)
    {
        text.text = time.FormatToTimer();

        if (_medals.Count == 0)
        {
            icon.color = Color.Lerp(icon.color, failureColor, Time.deltaTime * colorLerpSpeed);
            if (useFailureTextColor)
                text.color = Color.Lerp(text.color, failureColor, Time.deltaTime * colorLerpSpeed);
            return;
        }

        var currentIndex = _medals.FindIndex(m => time <= m.Time);
        Color newTargetColor;
        Color newNextColor;

        if (currentIndex == -1)
        {
            newTargetColor = failureColor;
            newNextColor = failureColor;
        }
        else
        {
            newTargetColor = _medals[currentIndex].Color;
            newNextColor = (currentIndex + 1 < _medals.Count)
                ? _medals[currentIndex + 1].Color
                : failureColor;
        }

        // Only change target when medal tier changes
        if (newTargetColor != _currentTargetColor)
        {
            _previousTargetColor = icon.color;
            _currentTargetColor = newTargetColor;
            _nextThresholdColor = newNextColor;
        }

        if (!_useBlinkColor)
        {
            icon.color = Color.Lerp(icon.color, _currentTargetColor, Time.deltaTime * colorLerpSpeed);

            if (useSameColor)
            {
                var targetTextColor = (currentIndex == -1 && useFailureTextColor)
                    ? failureColor
                    : _currentTargetColor;

                text.color = Color.Lerp(text.color, targetTextColor, Time.deltaTime * colorLerpSpeed);
            }
        }

        var timeToNext = NextThresholdTime(time);
        if (timeToNext <= timeToBlink)
        {
            if (_blinkCoroutine == null)
                _blinkCoroutine = StartCoroutine(BlinkColor());
        }
        else
        {
            if (_blinkCoroutine != null)
            {
                StopCoroutine(_blinkCoroutine);
                _blinkCoroutine = null;
                icon.color = _currentTargetColor;
                text.color = TextColor();
            }
        }
    }
    
    private float NextThresholdTime(float time)
    {
        foreach (var medal in _medals)
        {
            if (time <= medal.Time)
                return medal.Time - time;
        }
        return float.PositiveInfinity;
    }
    
    private IEnumerator BlinkColor()
    {
        _useBlinkColor = false;

        while (true)
        {
            _useBlinkColor = !_useBlinkColor;
            
            // Decide blink color: fixed or next threshold
            var blinkTarget = useNextThresholdColor ? _nextThresholdColor : blinkColor;
            var target = _useBlinkColor ? blinkTarget : _currentTargetColor;
            var targetText = _useBlinkColor ? blinkTarget : TextColor();

            icon.color = target;
            text.color = targetText;

            yield return new WaitForSeconds(blinkInterval);
        }
    }

    private Color TextColor() => useSameColor ? _currentTargetColor : _textStartColor;

    private void OnDestroy()
    {
        GameEntry.LoadingState.DetachOnPreload(_onPreload);
        GameEntry.LoadingState.DetachOnReady(_onReady);
        
        GlobalEvents.TimeUpdated.Detach(_timerObserver);
        StopAllCoroutines();
        _medals.Clear();
        _timerObserver.Dispose();
    }
}
