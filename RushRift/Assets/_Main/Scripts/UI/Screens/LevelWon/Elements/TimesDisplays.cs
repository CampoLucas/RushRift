using System;
using System.Collections;
using System.Collections.Generic;
using Game;
using Game.UI.Animations;
using Game.UI.Screens;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public sealed class TimesDisplays : MonoBehaviour
{
    [SerializeField] private LevelWonPresenter presenter;
    
    [Header("References")]
    [SerializeField] private TMP_Text currentTimeText;
    [SerializeField] private TMP_Text bestTimeText;

    [Header("Animation Settings")]
    [SerializeField] private float duration;

    [Header("Events")]
    [SerializeField] private UnityEvent onCompleted = new UnityEvent();
    [SerializeField] private UnityEvent onNewRecord = new UnityEvent();

    private Coroutine _coroutine;

    public void Play()
    {
        var model = presenter.GetModel();
        Play(model.EndTime, model.BestTime);
    }
    
    /// <summary>
    /// Plays the win screen animation.
    /// </summary>
    /// <param name="currentTime">The player's run time (seconds).</param>
    /// <param name="bestTime">The best recorded time (seconds).</param>
    public void Play(float currentTime, float bestTime)
    {
        // Stop old animations if any
        if (_coroutine != null)
            StopCoroutine(_coroutine);

        // Animate current time
        _coroutine = StartCoroutine(AnimateFullTimer(currentTimeText, bestTimeText, currentTime, bestTime));
    }

    private IEnumerator AnimateFullTimer(TMP_Text currText, TMP_Text bestText, float currTime, float bestTime)
    {
        yield return AnimateTimer(currText, currTime);
        yield return AnimateTimer(bestText, bestTime);
        
        onCompleted?.Invoke();

        if (currTime <= bestTime)
        {
            onNewRecord?.Invoke();
        }
    }
    
    private IEnumerator AnimateTimer(TMP_Text text, float targetTime)
    {
        var elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);

            var displayTime = Mathf.Lerp(0f, targetTime, t);
            text.text = FormatTime(displayTime);

            yield return null;
        }

        // Make sure it ends exactly at the target time
        text.text = FormatTime(targetTime);
        
        
    }

    private string FormatTime(float time)
    {
        var minutes = Mathf.FloorToInt(time / 60f);
        var seconds = Mathf.FloorToInt(time % 60f);
        var milliseconds = Mathf.FloorToInt((time * 1000f) % 1000f);

        return $"{minutes:00}:{seconds:00}.{milliseconds:000}";
    }
    
}
