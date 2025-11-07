using System;
using Game.DesignPatterns.Observers;
using Game.Entities.Components;
using Game.Utils;
using Game.VFX;
using UnityEngine;

namespace Game.Entities
{
    public class LaserView : EntityView
    {
        [SerializeField] private LaserVFXController laserVfx;

        [Header("On Destroy")]
        [SerializeField] private VFXPrefabID destroyVFX = VFXPrefabID.Explosion;
        [SerializeField] private Vector3 offset;
        [SerializeField] private float scale = 1;

        private IController _controller;
        private ISubject<Vector3> _setLengthSubject;
        private ISubject<Vector3> _activateSubject;
        private ISubject<Vector3> _deactivateSubject;
        private ISubject _onDestroySubject;
        
        private ActionObserver<Vector3> _setLengthObserver;
        private ActionObserver<Vector3> _activateEffectObserver;
        private ActionObserver<Vector3> _deactivateEffectObserver;
        private ActionObserver _destroyVFXObserver;

        private void Awake()
        {
            _controller = GetComponent<IController>();
            _setLengthObserver = new ActionObserver<Vector3>(SetLaserLengthHandler);
            _activateEffectObserver = new ActionObserver<Vector3>(ActivateEffectHandler);
            _deactivateEffectObserver = new ActionObserver<Vector3>(DeactivateEffectHandler);
            _destroyVFXObserver = new ActionObserver(DestroyVFXHandler);
        }

        private void Start()
        {
            if (_controller == null) return;
            var model = _controller.GetModel();
            
            if (model.TryGetComponent<LaserComponent>(out var laser))
            {
                _setLengthSubject = laser.SetLengthSubject;
                _setLengthSubject.Attach(_setLengthObserver);

                _activateSubject = laser.OnActivateSubject;
                _activateSubject.Attach(_activateEffectObserver);
                
                _deactivateSubject = laser.OnDeactivateSubject;
                _deactivateSubject.Attach(_deactivateEffectObserver);
            }

            if (model.TryGetComponent<HealthComponent>(out var healthComponent))
            {
                _onDestroySubject = healthComponent.OnEmptyValue;
                _onDestroySubject.Attach(_destroyVFXObserver);
            }
        }

        private void SetLaserLengthHandler(Vector3 endPos)
        {
            laserVfx.SetEndPos(endPos);
        }

        private void ActivateEffectHandler(Vector3 endPos)
        {
            laserVfx.gameObject.SetActive(true);
            laserVfx.SetEndPos(endPos);
        }

        private void DeactivateEffectHandler(Vector3 endPos)
        {
            laserVfx.gameObject.SetActive(false);
            //laserVfx.SetEndPos(endPos);
        }

        private void DestroyVFXHandler()
        {
            var tr = transform;
            
            EffectManager.TryGetVFX(destroyVFX, new VFXEmitterParams()
            {
                position = tr.GetOffsetPos(offset),
                rotation = tr.rotation,
                scale = tr.localScale.magnitude * scale
            }, out var emitter);
        }

        protected override void OnDispose()
        {
            _controller = null;
            
            _setLengthSubject?.Detach(_setLengthObserver);
            _setLengthSubject = null;
            
            _activateSubject?.Detach(_activateEffectObserver);
            _activateSubject = null;
            
            _deactivateSubject?.Detach(_deactivateEffectObserver);
            _deactivateSubject = null;
            
            _onDestroySubject?.Detach(_destroyVFXObserver);
            _onDestroySubject = null;
            
            _setLengthObserver?.Dispose();
            _setLengthObserver = null;
            
            _activateEffectObserver?.Dispose();
            _activateEffectObserver = null;
            
            _deactivateEffectObserver?.Dispose();
            _deactivateEffectObserver = null;
            
            _destroyVFXObserver?.Dispose();
            _destroyVFXObserver = null;
        }
    }
}