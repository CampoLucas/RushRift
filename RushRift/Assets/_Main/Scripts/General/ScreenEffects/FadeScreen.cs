using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.ScreenEffects
{
    public class FadeScreen : MonoBehaviour
    {
        [SerializeField] private float speed = 1;
        [SerializeField] private Material material;
        
        private static readonly int Amount = Shader.PropertyToID("_FadeAmount");
        private float FadeAmount { set => material.SetFloat(Amount, value); }
        
        private Coroutine _coroutine;
        private bool _destroyed;

        private void Awake()
        {
            _destroyed = false;
            FadeIn();
        }

        public void FadeIn()
        {
            if (_destroyed) return;
            if (_coroutine != null) StopCoroutine(_coroutine);
            _coroutine = StartCoroutine(Interpolate(1, 0));
        }
        
        public void FadeOut()
        {
            if (_destroyed) return;
            if (_coroutine != null) StopCoroutine(_coroutine);
            _coroutine = StartCoroutine(Interpolate(0, 1));
        }
        
        private IEnumerator Interpolate(float from, float to)
        {
            var curr = from;
            
            for (float t = 0; Math.Abs(curr - to) > .01f; t += Time.deltaTime * speed)
            {
                curr = Mathf.SmoothStep(from, to, t);
                FadeAmount = curr;
                yield return null;
            }
        }

        private void OnDestroy()
        {
            _destroyed = true;
            StopAllCoroutines();

            material = null;
            _coroutine = null;
        }
    }
}
