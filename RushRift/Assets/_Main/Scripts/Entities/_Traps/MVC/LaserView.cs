using System;
using Game.DesignPatterns.Observers;
using Game.Entities.Components;
using Game.VFX;
using UnityEngine;

namespace Game.Entities
{
    public class LaserView : EntityView
    {
        [SerializeField] private Transform laserEnd;
        [SerializeField] private LaserVFXController laserVfx;

        private IController _controller;
        private ActionObserver<Vector3> _setLengthObserver;
        private ISubject<Vector3> _setLengthSubject;

        private void Awake()
        {
            _controller = GetComponent<IController>();
            _setLengthObserver = new ActionObserver<Vector3>(SetLaserLengthHandler);
        }

        private void Start()
        {
            if (_controller.GetModel().TryGetComponent<LaserComponent>(out var laser))
            {
                _setLengthSubject = laser.SetLengthSubject;
                _setLengthSubject.Attach(_setLengthObserver);
            }
        }

        private void SetLaserLengthHandler(Vector3 endPos)
        {
            laserEnd.transform.position = endPos;
            laserVfx.SetEndPos(endPos);
        }

        protected override void OnDispose()
        {
            _setLengthSubject?.Detach(_setLengthObserver);
            _setLengthSubject = null;
            
            _setLengthObserver?.Dispose();
            _setLengthObserver = null;
        }
    }
}