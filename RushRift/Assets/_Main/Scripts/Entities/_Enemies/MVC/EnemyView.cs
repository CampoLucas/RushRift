using System;
using Game.DesignPatterns.Observers;
using Game.Entities.Components;
using Game.Utils;
using Game.VFX;
using UnityEngine;

namespace Game.Entities
{
    public class EnemyView : EntityView
    {
        [Header("On Destroy")]
        [SerializeField] private VFXEmitterEntry[] destroyVFXs;
        
        private IController _controller;
        private ISubject _onDestroySubject;
        private ActionObserver _destroyVFXObserver;

        protected override void OnAwake()
        {
            _controller = GetComponent<IController>();
            _destroyVFXObserver = new ActionObserver(DestroyVFXHandler);
        }
        
        private void Start()
        {
            if (_controller == null) return;
            var model = _controller.GetModel();
            
            if (model.TryGetComponent<HealthComponent>(out var healthComponent))
            {
                _onDestroySubject = healthComponent.OnEmptyValue;
                _onDestroySubject.Attach(_destroyVFXObserver);
            }
        }
        
        private void DestroyVFXHandler()
        {
            foreach (var vfx in destroyVFXs)
            {
                vfx.GetVFXEmitter(Transform);
            }

            // EffectManager.TryGetVFX(destroyVFX, new VFXEmitterParams()
            // {
            //     position = tr.GetOffsetPos(offset),
            //     rotation = tr.rotation,
            //     scale = tr.localScale.magnitude * scale
            // }, out var emitter);
        }
        
        protected override void OnDispose()
        {
            _controller = null;
            
            _onDestroySubject?.Detach(_destroyVFXObserver);
            _onDestroySubject = null;
            
            _destroyVFXObserver?.Dispose();
            _destroyVFXObserver = null;
        }
    }
}