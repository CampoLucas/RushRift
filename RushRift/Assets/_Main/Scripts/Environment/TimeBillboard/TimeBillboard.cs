using System;
using Game.DesignPatterns.Observers;
using MyTools.Utils;
using TMPro;
using UnityEngine;

namespace Game.Enviroment
{
    public class TimeBillboard : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;

        private ActionObserver<float> _updateTimeObserver;

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
            text.text = time.FormatToTimer();
        }

        private void OnDestroy()
        {
            if (LevelManager.TryGetTimerSubject(out var subject))
            {
                subject.Detach(_updateTimeObserver);
            }
        }
    }
}