using System;
using System.Collections;
using Game;
using Game.DesignPatterns.Observers;
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
    
    [Header("Blink")]
    [SerializeField] private Color blinkColor;
    [SerializeField] private float blinkInterval = 0.5f;

    private float _goldThreshold = -1;
    private float _silverThreshold = -1;
    private float _bronzeThreshold = -1;

    private Coroutine _blinkCoroutine;
    private Color _currentTargetColor;

    private ActionObserver<float> _timerObserver;

    private void Awake()
    {
        _timerObserver = new ActionObserver<float>(OnTimeUpdated);
    }

    private void Start()
    {
        LevelManager.OnTimeUpdated.Attach(_timerObserver);
        
        _goldThreshold = _silverThreshold = _bronzeThreshold = float.PositiveInfinity;

        if (!LevelManager.TryGetLevelConfig(out var levelConfig) && levelConfig)
        {
#if UNITY_EDITOR
            Debug.LogError("ERROR: TimerDisplay couldn't find the level config");
#endif
        }
        
        _goldThreshold   = Mathf.Max(0f, levelConfig.Gold.requiredTime);
        _silverThreshold = Mathf.Max(0f, levelConfig.Silver.requiredTime);
        _bronzeThreshold = Mathf.Max(0f, levelConfig.Bronze.requiredTime);
    }

    private void OnTimeUpdated(float time)
    {
        // Update formatted text
        text.text = time.FormatToTimer();
        
        // Determine the target color based on thresholds
        if (time <= _goldThreshold) _currentTargetColor = goldColor;
        else if (time <= _silverThreshold) _currentTargetColor = silverColor;
        else if (time <= _bronzeThreshold) _currentTargetColor = bronzeColor;
        else _currentTargetColor = failureColor;

        // Smoothly transition to the target color
        icon.color = Color.Lerp(icon.color, _currentTargetColor, Time.deltaTime * 10f);
        text.color = Color.Lerp(text.color, _currentTargetColor, Time.deltaTime * 10f);
        
        // Start/stop blink when time is below 5 seconds
        if (time <= 5f)
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
                text.color = _currentTargetColor;
            }
        }
    }
    
    private IEnumerator BlinkColor()
    {
        bool useBlinkColor = false;

        while (true)
        {
            useBlinkColor = !useBlinkColor;
            Color target = useBlinkColor ? blinkColor : _currentTargetColor;

            icon.color = target;
            text.color = target;

            yield return new WaitForSeconds(blinkInterval);
        }
    }

    private void OnDestroy()
    {
        LevelManager.OnTimeUpdated.Detach(_timerObserver);
        StopAllCoroutines();
        
        _timerObserver.Dispose();
    }
}
