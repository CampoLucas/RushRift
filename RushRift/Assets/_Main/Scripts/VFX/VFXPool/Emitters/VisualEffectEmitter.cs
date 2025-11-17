using System;
using UnityEngine;
using UnityEngine.VFX;

namespace Game
{
    public class VisualEffectEmitter : VFXEmitter
    {
        [SerializeField] private VisualEffect effect;
        [SerializeField] private float duration = .4f;

        private float _timer;
        
        private void Update()
        {
            if (_timer <= 0)
            {
                Pool.Recycle(this);
            }

            _timer -= Time.deltaTime;
        }

        protected override void OnPoolDisable()
        {
            effect.Stop();
        }

        protected override void OnPoolReset()
        {
            _timer = duration;
            effect.Play();
        }

        protected override void OnDispose()
        {
            effect = null;
        }
    }
}