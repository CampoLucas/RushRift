using Game.DesignPatterns.Pool;
using Game.UI;
using UnityEngine;
using Game;
using Game.DesignPatterns.Observers;

namespace Game.Entities
{
    public class ParticleEmitter : VFXEmitter
    {
        [SerializeField] private ParticleSystem particle;

        private bool _wasPlaying;
        private bool _pauseAttached;
        private NullCheck<ActionObserver<bool>> _pauseObserver;
        private NullCheck<ActionObserver<bool>> _loadingObserver;

        protected override void OnAwake()
        {
            if (_loadingObserver.TryGet(out var observer, () => new ActionObserver<bool>(OnLoading)))
            {
                GameEntry.LoadingState.AttachOnLoading(observer);
            }
        }

        private void Update()
        {
            if (!PauseHandler.IsPaused && !particle.IsAlive(true))
            {
                Pool.Recycle(this);
            }
        }

        protected override void OnPoolDisable()
        {
            _wasPlaying = false;
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
            if (!_pauseAttached && _pauseObserver.TryGet(out var observer, () => new ActionObserver<bool>(OnPause)))
            {
                _pauseAttached = true;
                PauseHandler.Attach(observer);
                //OnPause(PauseHandler.IsPaused);
            }
        }

        private void OnDisable()
        {
            if (_pauseAttached && _pauseObserver.TryGet(out var observer))
            {
                _pauseAttached = false;
                PauseHandler.Detach(observer);
            }
        }

        private void OnLoading(bool loading)
        {
            if (loading)
            {
                if (_pauseAttached && _pauseObserver.TryGet(out var observer))
                {
                    _pauseAttached = false;
                    PauseHandler.Detach(observer);
                }
            }
            else
            {
                if (!_pauseAttached && _pauseObserver.TryGet(out var observer, () => new ActionObserver<bool>(OnPause)))
                {
                    _pauseAttached = true;
                    PauseHandler.Attach(observer);
                    //OnPause(PauseHandler.IsPaused);
                }
            }
        }

        private void OnPause(bool pause)
        {
            if (particle == null) return;

            if (pause)
            {
                _wasPlaying = particle.isPlaying;
                particle.Pause(true);
            }
            else if (_wasPlaying)
            {
                particle.Play(true);
            }
        }
    }
}