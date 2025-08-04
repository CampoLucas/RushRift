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
        
        private float FadeAmount { set => material.SetFloat("_FadeAmount", value); }
        
        private Coroutine _coroutine;

        private void Awake()
        {
            FadeIn();
        }

        public void FadeIn()
        {
            if (_coroutine != null) StopCoroutine(_coroutine);
            _coroutine = StartCoroutine(Interpolate(1, 0));
        }
        
        public void FadeOut()
        {
            if (_coroutine != null) StopCoroutine(_coroutine);
            _coroutine = StartCoroutine(Interpolate(0, 1));
        }
        
        private IEnumerator Interpolate(float from, float to)
        {
            var curr = from;
            
            for (float t = 0; curr != to; t += Time.deltaTime * speed)
            {
                curr = Mathf.SmoothStep(from, to, t);
                FadeAmount = curr;
                yield return null;
            }
        }
    }
}
