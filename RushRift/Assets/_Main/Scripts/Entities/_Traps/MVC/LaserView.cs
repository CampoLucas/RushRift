using System;
using Game.DesignPatterns.Observers;
using Game.Entities.Components;
using Game.VFX;
using UnityEngine;

namespace Game.Entities
{
    public class LaserView : EntityView
    {
        [SerializeField] private LaserVFXController laserVfx;

        private IController _controller;
        private ISubject<Vector3> _setLengthSubject;
        private ISubject<Vector3> _activateSubject;
        private ISubject<Vector3> _deactivateSubject;
        
        private ActionObserver<Vector3> _setLengthObserver;
        private ActionObserver<Vector3> _activateEffectObserver;
        private ActionObserver<Vector3> _deactivateEffectObserver;

        private void Awake()
        {
            _controller = GetComponent<IController>();
            _setLengthObserver = new ActionObserver<Vector3>(SetLaserLengthHandler);
            _activateEffectObserver = new ActionObserver<Vector3>(ActivateEffectHandler);
            _deactivateEffectObserver = new ActionObserver<Vector3>(DeactivateEffectHandler);
        }

        private void Start()
        {
            if (_controller != null && _controller.GetModel().TryGetComponent<LaserComponent>(out var laser))
            {
                _setLengthSubject = laser.SetLengthSubject;
                _setLengthSubject.Attach(_setLengthObserver);

                _activateSubject = laser.OnActivateSubject;
                _activateSubject.Attach(_activateEffectObserver);
                
                _deactivateSubject = laser.OnDeactivateSubject;
                _deactivateSubject.Attach(_deactivateEffectObserver);
            }
        }

        private void SetLaserLengthHandler(Vector3 endPos)
        {
            laserVfx.SetEndPos(endPos);
        }

        private void ActivateEffectHandler(Vector3 endPos)
        {
            Debug.Log("SuperTest: Laser on view");
            laserVfx.gameObject.SetActive(true);
            laserVfx.SetEndPos(endPos);
        }

        private void DeactivateEffectHandler(Vector3 endPos)
        {
            Debug.Log("SuperTest: Laser off view");
            laserVfx.gameObject.SetActive(false);
            //laserVfx.SetEndPos(endPos);
        }

        protected override void OnDispose()
        {
            _setLengthSubject?.Detach(_setLengthObserver);
            _setLengthSubject = null;
            
            _activateSubject?.Detach(_activateEffectObserver);
            _activateSubject = null;
            
            _deactivateSubject?.Detach(_deactivateEffectObserver);
            _deactivateSubject = null;
            
            _setLengthObserver?.Dispose();
            _setLengthObserver = null;
            
            _activateEffectObserver?.Dispose();
            _activateEffectObserver = null;
            
            _deactivateEffectObserver?.Dispose();
            _deactivateEffectObserver = null;
        }
    }
}