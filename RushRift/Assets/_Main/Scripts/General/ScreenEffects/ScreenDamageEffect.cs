using System;
using System.Collections;
using UnityEngine;

namespace Game.ScreenEffects
{
    public class ScreenDamageEffect : MonoBehaviour
    {
        private static readonly int Radius = Shader.PropertyToID("_VignetteRadius");
        private float EffectRadius { get => material.GetFloat(Radius); set => material.SetFloat(Radius, value); }
        
        [Header("Material Reference")]
        [SerializeField] private Material material;

        [Header("Effect Settings")]
        [SerializeField] private float inSpeed = 5;
        [SerializeField] private float outSpeed = .5f;
        
        private Coroutine _coroutine;
        private int _stacks;
        private bool _destroyed;
        // private IObserver _onPaused;
        // private IObserver _onUnPaused;
        
        private void Awake()
        {
            _destroyed = false;
            // _onPaused = new ActionObserver(OnPausedHandler);
            // _onUnPaused = new ActionObserver(OnUnPausedHandler);
        }
        
        // private void Start()
        // {
        //     // UIManager.OnPaused.Attach(_onPaused);
        //     // UIManager.OnUnPaused.Attach(_onUnPaused);
        // }

        private void OnPausedHandler()
        {
            // ToDo: cach effect values and stop coroutine 
        }
        
        private void OnUnPausedHandler()
        {
            // ToDo: resume the coroutine with cached values
        }

        // private void Update()
        // {
        //     if (UnityEngine.Input.GetMouseButtonDown(1))
        //     {
        //         DoEffect(1);
        //     }
        // }

        public void DoEffect(float intensity)
        {
            if (_destroyed) return;
            if (_coroutine != null) StopCoroutine(_coroutine);
            _coroutine = StartCoroutine(ScreenDamage(intensity, EffectRadius));
        }
        
        private IEnumerator ScreenDamage(float intensity, float startValue = 1)
        {
            var targetRadius = Remap(intensity * _stacks, 0, 1, .4f, -.15f);
            _stacks++;
            var currRadius = startValue; // No damage
            
            // in animation
            for (float t = 0; Math.Abs(currRadius - targetRadius) > .01f; t += Time.deltaTime * inSpeed)
            {
                currRadius = Mathf.Lerp(1, targetRadius, t);
                EffectRadius = currRadius;
                yield return null;
            }

            EffectRadius = targetRadius;
            
            // out animation
            for (float t = 0; currRadius < 1; t += Time.deltaTime * outSpeed)
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
