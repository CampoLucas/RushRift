using System;
using Game.DesignPatterns.Pool;
using UnityEngine;

namespace Game.Entities
{
    public class ParticleEmitter : VFXEmitter
    {
        [SerializeField] private ParticleSystem particle;
        
        private void Update()
        {
            if (particle.time >= particle.main.duration)
            {
                Pool.Recycle(this);
            }
        }

        protected override void OnPoolDisable()
        {
            particle.Stop();
        }

        protected override void OnPoolReset()
        {
            particle.Play();
        }

        protected override void OnDispose()
        {
            particle = null;
        }
    }
}

