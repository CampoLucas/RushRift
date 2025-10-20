using System;
using System.Collections;
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
    [SerializeField] private Color bronzeColor;
    [SerializeField] private Color silverColor;
    [SerializeField] private Color goldColor;
    [SerializeField] private Color failureColor;

    [Header("Settings")]
    [Tooltip("Makes the text have the same color as the medals.")] 
    [SerializeField] private bool useSameColor;
    [Tooltip("Use the next threshold color instead of the blinkColor when blinking.")]
    [SerializeField] private bool useNextThresholdColor = true;
    [Tooltip("Makes the text use failure color when passing the failure threshold.")]
    [SerializeField] private bool useFailureTextColor = true;
    [SerializeField] private Color blinkColor;
    [SerializeField] private float blinkInterval = 0.5f;
    [SerializeField] private float timeToBlink = 5;

    private float _goldThreshold = -1;
    private float _silverThreshold = -1;
    private float _bronzeThreshold = -1;

    private Coroutine _blinkCoroutine;
    private Color _currentTargetColor;
    private Color _textStartColor;
    private Color _nextThresholdColor = Color.white; // for blinking

    private ActionObserver<BaseLevelSO> _onLevelLoadingStart;
    private ActionObserver<BaseLevelSO> _onLevelLoadingEnd;
    
    private ActionObserver<float> _timerObserver;

    private bool _useBlinkColor;

    private void Awake()
    {
        if (_timerObserver == null)
        {
            _timerObserver = new ActionObserver<float>(OnTimeUpdated);
        }

        _onLevelLoadingStart = new ActionObserver<BaseLevelSO>(OnLoadingStartHandler);
        _onLevelLoadingEnd = new ActionObserver<BaseLevelSO>(OnLoadingEndHandler);
        
        _textStartColor = text.color;
        

        GameEntry.LoadingLevelStart.Attach(_onLevelLoadingStart);
        GameEntry.LoadingLevelEnd.Attach(_onLevelLoadingEnd);
    }

    private void OnLoadingStartHandler(BaseLevelSO level)
    {
        StopAllCoroutines();
        if (_timerObserver == null)
        {
            _timerObserver = new ActionObserver<float>(OnTimeUpdated);
        }
        GlobalEvents.TimeUpdated.Detach(_timerObserver);
    }
    
    private void OnLoadingEndHandler(BaseLevelSO level)
    {
        StopAllCoroutines();
        if (_timerObserver == null)
        {
            _timerObserver = new ActionObserver<float>(OnTimeUpdated);
        }
        GlobalEvents.TimeUpdated.Attach(_timerObserver);
        
        _goldThreshold = _silverThreshold = _bronzeThreshold = float.PositiveInfinity;

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

        Debug.Log("[SuperTest] Found the level config");
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        
        _goldThreshold = Mathf.Max(0f, level.GetMedal(MedalType.Gold).requiredTime);
        _silverThreshold = Mathf.Max(0f, level.GetMedal(MedalType.Silver).requiredTime);
        _bronzeThreshold = Mathf.Max(0f, level.GetMedal(MedalType.Bronze).requiredTime);
    }

    private void OnTimeUpdated(float time)
    {
        // Update formatted text
        text.text = time.FormatToTimer();
        
        // Determine the target color based on thresholds
        if (time <= _goldThreshold)
        {
            _currentTargetColor = goldColor;
            _nextThresholdColor = silverColor;
        }
        else if (time <= _silverThreshold)
        {
            _currentTargetColor = silverColor;
            _nextThresholdColor = bronzeColor;
        }
        else if (time <= _bronzeThreshold)
        {
            _currentTargetColor = bronzeColor;
            _nextThresholdColor = failureColor;
        }
        else
        {
            _currentTargetColor = failureColor;
        }

        // Smoothly transition to the target color
        if (!_useBlinkColor)
        {
            icon.color = Color.Lerp(icon.color, _currentTargetColor, Time.deltaTime * 10f);
            
            if (useSameColor)
            {
                if (time > _bronzeThreshold && useFailureTextColor)
                    text.color = failureColor;
                else
                    text.color = Color.Lerp(text.color, _currentTargetColor, Time.deltaTime * 10f);
            }
        }
        
        // Start/stop blink when time is below 5 seconds
        var timeToNextThreshold = NextThresholdTime(time);
        if (timeToNextThreshold <= timeToBlink)
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
        if (time <= _goldThreshold) return _goldThreshold - time;
        if (time <= _silverThreshold) return _silverThreshold - time;
        if (time <= _bronzeThreshold) return _bronzeThreshold - time;
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
        GlobalEvents.TimeUpdated.Detach(_timerObserver);
        StopAllCoroutines();
        
        _timerObserver.Dispose();
    }
}
