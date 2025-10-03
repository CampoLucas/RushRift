using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Game.UI.Group
{
    public class UIGroupAnimation : UIAnimation
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private List<UIGroupData> animations;
        
        [Header("Events")]
        [SerializeField] private UnityEvent onPlaySequences = new UnityEvent();
        [SerializeField] private UnityEvent onAllSequencesComplete = new UnityEvent();

        private Coroutine _coroutine;

        private void Awake()
        {
            if (canvas)
            {
                canvas.enabled = false;
            }
        }

        public override void Play(float delay)
        {
            Stop();
            
            _coroutine = StartCoroutine(PlayRoutine(delay));
        }

        public override IEnumerator PlayRoutine(float delay)
        {
            if (delay > 0)
            {
                yield return new WaitForSeconds(delay);
            }

            if (canvas)
            {
                canvas.enabled = true;
            }
            onPlaySequences?.Invoke();
            
            var running = new List<Coroutine>();

            // Start all sequences in parallel
            foreach (var sequence in animations)
            {
                running.Add(StartCoroutine(sequence.Animation.PlayRoutine(sequence.Delay)));
            }

            // Wait for all to finish
            foreach (var coroutine in running)
            {
                yield return coroutine;
            }
            
            onAllSequencesComplete?.Invoke();
        }

        public override void Stop()
        {
            if (_coroutine == null) return;
            
            StopCoroutine(_coroutine);
            for (var i = 0; i < animations.Count; i++)
            {
                animations[i].Animation.Stop();
            }
        }
    }
}