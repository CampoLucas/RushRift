using System;
using System.Collections;
using Game.DesignPatterns.Observers;
using UnityEngine;

namespace Game.ScreenEffects
{
    public class ScreenBlur : MonoBehaviour
    {
        private static readonly int Radius = Shader.PropertyToID("_Radius");

        [Header("Material Reference")]
        [SerializeField] private Material material;
        
        [Header("Effect Settings")]
        [SerializeField] private float inSpeed = 5;
        [SerializeField] private float outSpeed = .5f;
        [SerializeField] private float inDuration = .1f;
        [SerializeField] private float outDuration = .5f;

        private float EffectRadius
        {
            get => material.GetFloat(Radius);
            set => material.SetFloat(Radius, value);
        }
        
        private Coroutine _coroutine;
        private int _stacks;
        private bool _destroyed;
        private IObserver<float, float> _doEffectObserver;

        private void Awake()
        {
            _doEffectObserver = new ActionObserver<float, float>(DoEffect);
        }

        private void Start()
        {
            EffectRadius = 1;
            
            EffectManager.AttachBlur(_doEffectObserver);
        }
        
        private void DoEffect(float duration, float magnitude)
        {
            if (_destroyed) return;
            if (_coroutine != null) StopCoroutine(_coroutine);
            _coroutine = StartCoroutine(ScreenDamage(magnitude, EffectRadius, inDuration, outDuration));
        }
        
        private IEnumerator ScreenDamage(float intensity, float startValue = 1, float inDur = .1f, float outDur = 1)
        {
            var targetRadius = Remap(intensity * _stacks, 0, 1, .4f, -.15f);
            _stacks++;
            var currRadius = startValue;
            
            // in animation
            for (float t = 0; Math.Abs(currRadius - targetRadius) > inDur; t += Time.deltaTime * inSpeed)
            {
                currRadius = Mathf.Lerp(currRadius, targetRadius, t);
                EffectRadius = currRadius;
                yield return null;
            }

            EffectRadius = targetRadius;
            
            // out animation
            for (float t = 0; currRadius < outDur; t += Time.deltaTime * outSpeed)
            {
                currRadius = Mathf.Lerp(targetRadius, 1, t);
                EffectRadius = currRadius;
                yield return null;
            }

            EffectRadius = 1;

            _stacks = 0;
        }

        private float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            return Mathf.Lerp(toMin, toMax, Mathf.InverseLerp(fromMin, fromMax, value));
        }
        
        private void OnDestroy()
        {
            _destroyed = true;
            StopAllCoroutines();
            
            // UIManager.OnPaused.Detach(_onPaused);
            // UIManager.OnUnPaused.Detach(_onUnPaused);

            // _onPaused?.Dispose();
            // _onPaused = null;
            // _onUnPaused?.Dispose();
            // _onUnPaused = null;
            _coroutine = null;
            material = null;
        }
    }
}