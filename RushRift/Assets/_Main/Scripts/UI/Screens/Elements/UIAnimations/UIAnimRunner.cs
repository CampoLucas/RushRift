using System;
using System.Collections;
using System.Collections.Generic;
using Game.UI.Animations;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.UI
{
    public class UIAnimRunner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform targetRect;
        [SerializeField] private Graphic targetGraphic;
        [SerializeField] private TMP_Text targetTMP;

        [Header("Animations")]
        [SerializeField] private SequenceAnim[] sequences;

        [Header("Events")]
        [SerializeField] private UnityEvent onAllSequencesComplete = new UnityEvent();

        private Coroutine _runnerCoroutine;
        
        public void Play()
        {
            Stop();
            _runnerCoroutine = StartCoroutine(PlaySequences());
        }

        public void Stop()
        {
            if (_runnerCoroutine == null) return;
            StopCoroutine(_runnerCoroutine);
            _runnerCoroutine = null;
        }

        public void End()
        {
            
        }

        private IEnumerator PlaySequences()
        {
            if (sequences == null || sequences.Length == 0)
            {
                onAllSequencesComplete?.Invoke();
                yield break;
            }
            
            var running = new List<Coroutine>();

            // Start all sequences in parallel
            foreach (var sequence in sequences)
            {
                running.Add(StartCoroutine(PlaySequence(sequence)));
            }

            // Wait for all to finish
            foreach (var coroutine in running)
                yield return coroutine;
            
            // Trigger the completion event
            onAllSequencesComplete?.Invoke();
            _runnerCoroutine = null;
        }

        private IEnumerator PlaySequence(SequenceAnim sequence)
        {
            do
            {
                // Run all UIAnim in this sequence in parallel
                var running = new List<Coroutine>();
                foreach (var anim in sequence.anims)
                {
                    running.Add(StartCoroutine(PlayAnimation(anim, targetRect, targetGraphic)));
                }

                // Wait until all animations complete
                foreach (var coroutine in running)
                    yield return coroutine;

            } while (sequence.looping);
        }
        
        
        public IEnumerator PlayAnimation(RectTransform rect, Graphic graphic, UIAnim[] anims)
        {
            // Start all animations in parallel
            var running = new List<Coroutine>();

            for (var i = 0; i < anims.Length; i++)
            {
                running.Add(StartCoroutine(PlayAnimation(anims[i], rect, graphic)));
            }

            for (var i = 0; i < running.Count; i++)
            {
                yield return running[i];
            }
        }

        private IEnumerator PlayAnimation(UIAnim anim, RectTransform rect, Graphic graphic)
        {
            yield return new WaitForSeconds(anim.Delay);

            var time = 0f;
            while (time < anim.Duration)
            {
                var t = time / anim.Duration;

                switch (anim.Type)
                {
                    case UIAnimType.Move: MoveAnimation(anim, rect, t); break;
                    case UIAnimType.Scale: ScaleAnimation(anim, rect, t); break;
                    case UIAnimType.Rotate: RotationAnimation(anim, rect, t); break;
                    case UIAnimType.Color: ColorAnimation(anim, graphic, t); break;
                    default: throw new ArgumentOutOfRangeException();
                }

                time += Time.deltaTime;
                yield return null;
            }
            
            switch (anim.Type)
            {
                case UIAnimType.Move: rect.anchoredPosition = anim.TargetVector; break;
                case UIAnimType.Scale: rect.localScale = Vector3.one * anim.TargetFloat; break;
                case UIAnimType.Rotate: rect.localRotation = Quaternion.Euler(0, 0, anim.TargetFloat); break;
                case UIAnimType.Color: if (graphic != null) graphic.color = anim.TargetColor; break;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        private void MoveAnimation(in UIAnim anim, in RectTransform rect, in float t)
        {
            var pos = rect.anchoredPosition;
            var targetPos = anim.TargetVector;
            var curve = anim.Curve2.Evaluate(t);
            
            var x = Mathf.Lerp(pos.x, targetPos.x, curve.x);
            var y = Mathf.Lerp(pos.y, targetPos.y, curve.y);
            rect.anchoredPosition = new Vector2(x, y);
        }
        
        private void ScaleAnimation(in UIAnim anim, in RectTransform rect, in float t)
        {
            rect.localScale = Vector3.Lerp(rect.localScale, Vector3.one * anim.TargetFloat, anim.Curve.Evaluate(t));
        }

        private void RotationAnimation(in UIAnim anim, in RectTransform rect, in float t)
        {
            rect.localRotation = Quaternion.Lerp(rect.localRotation, Quaternion.Euler(0, 0, anim.TargetFloat), 
                anim.Curve.Evaluate(t));
        }

        private void ColorAnimation(in UIAnim anim, in Graphic graphic, in float t)
        {
            if (!graphic) return;
            Debug.Log("Change color");

            var curvedT = anim.Curve.Evaluate(t);

            // TMP_Text is also a Graphic, but needs special handling
            if (graphic is TMP_Text tmp)
            {
                tmp.color = Color.Lerp(tmp.color, anim.TargetColor, curvedT);
            }
            else
            {
                graphic.color = Color.Lerp(graphic.color, anim.TargetColor, curvedT);
            }
            
            return;
            
            var color = Color.Lerp(graphic.color, anim.TargetColor, anim.Curve.Evaluate(t));
            graphic.color = color;
            
            // if (graphic) 
            // else if (targetTMP) targetTMP.color = color;
        }
    }
}