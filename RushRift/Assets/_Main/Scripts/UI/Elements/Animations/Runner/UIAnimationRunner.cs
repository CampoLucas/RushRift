using System;
using System.Collections;
using System.Collections.Generic;
using Game.UI.Animations;
using MyTools.Global;
using TMPro;
using Tools.Scripts.Classes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.UI
{
    public class UIAnimationRunner : UIAnimation
    {
        private const int MAX_ANIMS = 25;
        
        [Header("Settings")]
        [SerializeField] private Vector2 offset;
        [SerializeField] private Vector2 playPosition;
        [SerializeField] private float playRotation;
        [SerializeField] private float playScale;
        [SerializeField] private Color playColor;

        [Header("References")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform targetRect;
        [SerializeField] private Graphic targetGraphic;

        [Header("Animations")]
        [SerializeField] private SequenceAnim[] sequences;

        [Header("Events")]
        [SerializeField] private UnityEvent onPlaySequences = new UnityEvent();
        [SerializeField] private UnityEvent onAllSequencesComplete = new UnityEvent();

        private Coroutine _runnerCoroutine;

        private void Awake()
        {
            Reset();
        }

        public override void Reset()
        {
            if (canvas)
            {
                canvas.enabled = false;
            }
        }

        public void SetPosition(Vector2 pos)
        {
            if (!targetRect)
            {
                return;
            }
        
            targetRect.anchoredPosition = pos + offset;
        }

        public void SetRotation(float rotation)
        {
            if (!targetRect)
            {
                return;
            }
            
            var rot = targetRect.localRotation.eulerAngles;
            targetRect.localRotation = Quaternion.Euler(rot.x, rot.y, rotation);
        }

        public void SetScale(float scale)
        {
            if (!targetRect)
            {
                return;
            }
            
            targetRect.localScale = Vector3.one * scale;
        }

        public void SetColor(Color color)
        {
            if (!targetGraphic)
            {
                return;
            }
            
            if (targetGraphic is TMP_Text tmp)
            {
                tmp.color = color;
            }
            else
            {
                targetGraphic.color = color;
            }
        }

        public override void Play(float delay)
        {
            StopCoroutine();
            
            _runnerCoroutine = StartCoroutine(DoAnim(playPosition, playRotation, playScale, playColor, delay));
        }

        public override IEnumerator PlayRoutine(float delay)
        {
            yield return DoAnim(playPosition, playRotation, playScale, playColor, delay);
        }

        private IEnumerator DoAnim(Vector2 startPos, float startRot, float startScale, Color startColor, float delay)
        {
            SetPosition(startPos);
            SetRotation(startRot);
            SetScale(startScale);
            SetColor(startColor);
            
            onPlaySequences?.Invoke();

            yield return PlaySequences(delay);
        }

        public override void Stop()
        {
            StopCoroutine();
            
            SetPosition(playPosition);
            SetRotation(playRotation);
            SetScale(playScale);
            SetColor(playColor);

            onAllSequencesComplete?.Invoke();
        }

        private void StopCoroutine()
        {
            if (_runnerCoroutine == null) return;
            StopCoroutine(_runnerCoroutine);
            _runnerCoroutine = null;
        }
        
        private Color GetColor()
        {
            return (targetGraphic is TMP_Text tmp) ? tmp.color : targetGraphic.color;
        }

        private IEnumerator PlaySequences(float delay)
        {
            if (delay > 0) yield return new WaitForSeconds(delay);
            
            if (canvas) canvas.enabled = true;
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
                yield return PlayRecursive(sequence.anims, 0);
            } while (sequence.looping);
        }

        private IEnumerator PlayRecursive(UIAnim[] anims, int index)
        {
            var maxAnims = index >= MAX_ANIMS;

            if (index >= anims.Length || maxAnims)
            {
                if (maxAnims)
                {
                    this.Log($"Maximum animations ({MAX_ANIMS}) in UIAnimationRunner reached.", LogType.Warning);
                }
                
                yield break;
            }

            var anim = anims[index];
            IEnumerator routine = null;

            switch (anim.Type)
            {
                case UIAnimType.Move:
                    routine = MoveRoutine(anim.Duration, anim.Delay, targetRect.anchoredPosition, anim.TargetVector, anim.Curve2);
                    break;
                case UIAnimType.Scale:
                    routine = ScaleRoutine(anim.Duration, anim.Delay, targetRect.localScale.x, anim.TargetFloat, anim.Curve);
                    break;
                case UIAnimType.Rotate:
                    routine = RotateRoutine(anim.Duration, anim.Delay, targetRect.localRotation.eulerAngles.z, anim.TargetFloat, anim.Curve);
                    break;
                case UIAnimType.Color:
                    routine = ColorRoutine(anim.Duration, anim.Delay, GetColor(), anim.TargetColor, anim.Curve);
                    break;
            }

            // Run the animation if any
            if (routine != null)
                yield return StartCoroutine(routine);

            // Play next recursively
            yield return StartCoroutine(PlayRecursive(anims, index + 1));
        }
        
        private IEnumerator MoveRoutine(float duration, float delay, Vector2 start, Vector2 end, Vector2Curve curve)
        {
            SetPosition(start);
            
            if (delay > 0) yield return new WaitForSeconds(delay);
            
            if (duration <= 0)
            {
                SetPosition(end);
                yield break;
            }
            
            var time = 0f;
            while (time < duration)
            {
                time += Time.deltaTime;
                var t = Mathf.Clamp01(time / duration);
                var c = curve.Evaluate(t);
                
                var x = Mathf.LerpUnclamped(start.x, end.x, c.x);
                var y = Mathf.LerpUnclamped(start.y, end.y, c.y);

                SetPosition(new Vector2(x, y));
                
                yield return null;
            }
            
            SetPosition(end);
        }
        
        private IEnumerator ScaleRoutine(float duration, float delay, float start, float end, AnimationCurve curve)
        {
            SetScale(start);
            
            if (delay > 0) yield return new WaitForSeconds(delay);
            
            if (duration <= 0)
            {
                SetScale(end);
                yield break;
            }
            
            var time = 0f;
            while (time < duration)
            {
                time += Time.deltaTime;
                var t = Mathf.Clamp01(time / duration);
                
                SetScale(Mathf.Lerp(start, end, curve.Evaluate(t)));
                
                yield return null;
            }
            
            SetScale(end);
        }
        
        private IEnumerator RotateRoutine(float duration, float delay, float start, float end, AnimationCurve curve)
        {
            SetRotation(start);
            
            if (delay > 0) yield return new WaitForSeconds(delay);
            
            if (duration <= 0)
            {
                SetRotation(end);
                yield break;
            }
            
            var time = 0f;
            while (time < duration)
            {
                time += Time.deltaTime;
                var t = Mathf.Clamp01(time / duration);
                
                SetRotation(Mathf.Lerp(start, end, curve.Evaluate(t)));
                
                yield return null;
            }
            
            SetRotation(end);
        }
        
        private IEnumerator ColorRoutine(float duration, float delay, Color start, Color end, AnimationCurve curve)
        {
            SetColor(start);
            
            if (delay > 0) yield return new WaitForSeconds(delay);
            
            if (duration <= 0)
            {
                SetColor(end);
                yield break;
            }
            
            var time = 0f;
            while (time < duration)
            {
                time += Time.deltaTime;
                var t = Mathf.Clamp01(time / duration);
                
                SetColor(Color.Lerp(start, end, curve.Evaluate(t)));
                
                yield return null;
            }
            
            SetColor(end);
        }
    }
}