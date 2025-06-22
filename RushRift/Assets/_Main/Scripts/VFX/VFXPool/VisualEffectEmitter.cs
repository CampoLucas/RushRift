using System;
using UnityEngine;
using UnityEngine.VFX;

namespace Game
{
    public class VisualEffectEmitter : VFXEmitter
    {
        [SerializeField] private VisualEffect effect;

        private bool _poolEnable;
        
        private void Update()
        {
            if (_poolEnable && !effect.HasAnySystemAwake())
            {
                Pool.Recycle(this);
            }
        }

        protected override void OnPoolDisable()
        {
            effect.Stop();
            _poolEnable = false;
        }

        protected override void OnPoolReset()
        {
            effect.Play();
            _poolEnable = true;
        }

        protected override void OnDispose()
        {
            effect = null;
        }
    }
}