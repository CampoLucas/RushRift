using System;
using System.Collections;
using Game.DesignPatterns.Observers;
using MyTools.Utils;
using TMPro;
using UnityEngine;

namespace Game.Enviroment
{
    public class TimeBillboard : MonoBehaviour
    {
        [Header("Reference")]
        [SerializeField] private TMP_Text text;

        private ActionObserver<float> _updateTimeObserver;
        private Coroutine _tickCoroutine;

        private void Awake()
        {
            _updateTimeObserver = new ActionObserver<float>(TimeUpdatedHandler);
        }

        private void Start()
        {
            if (LevelManager.TryGetTimerSubject(out var subject))
            {
                subject.Attach(_updateTimeObserver);
            }
        }

        private void TimeUpdatedHandler(float time)
        {
            var snappedTime = Mathf.Floor(time * 100f) / 100f; // snap to 0.01s
            text.text = snappedTime.FormatToClockTimer();
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            
            if (LevelManager.TryGetTimerSubject(out var subject))
            {
                subject.Detach(_updateTimeObserver);
            }
        }
    }
}