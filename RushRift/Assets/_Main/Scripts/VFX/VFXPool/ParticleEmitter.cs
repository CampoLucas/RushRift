using Game.DesignPatterns.Pool;
using Game.UI;
using UnityEngine;
using Game;
using Game.DesignPatterns.Observers;

namespace Game.Entities
{
    public class ParticleEmitter : EffectEmitter
    {
        [SerializeField] private ParticleSystem particle;

        private bool wasPlaying;
        private ActionObserver<bool> pauseObserver;

        private void Update()
        {
            if (!PauseHandler.IsPaused && !particle.IsAlive(true))
            {
                Pool.Recycle(this);
            }
        }

        protected override void OnPoolDisable()
        {
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        protected override void OnPoolReset()
        {
            particle.Play();
        }

        protected override void OnDispose()
        {
            particle = null;
        }

        private void OnEnable()
        {
            pauseObserver = new ActionObserver<bool>(OnPause);
            PauseHandler.Attach(pauseObserver);
            OnPause(PauseHandler.IsPaused);
        }

        private void OnDisable()
        {
            if (pauseObserver != null)
            {
                PauseHandler.Detach(pauseObserver);
                pauseObserver.Dispose();
                pauseObserver = null;
            }
        }

        private void OnPause(bool pause)
        {
            if (particle == null) return;

            if (pause)
            {
                wasPlaying = particle.isPlaying;
                particle.Pause(true);
            }
            else if (wasPlaying)
            {
                particle.Play(true);
            }
        }
    }
}