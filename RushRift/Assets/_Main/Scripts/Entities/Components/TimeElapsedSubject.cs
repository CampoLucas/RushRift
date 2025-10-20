using System;
using System.Collections;
using System.Collections.Generic;
using Game.DesignPatterns.Observers;
using UnityEngine;

namespace Game.Entities.Components
{
    public class TimeElapsedSubject : IEntityComponent, ISubject
    {
        private ISubject _subscribers = new Subject();
        private float _elapsedTime;
        private bool _active;
        private DesignPatterns.Observers.IObserver<float> _updateObserver;

        public TimeElapsedSubject()
        {
            _updateObserver = new ActionObserver<float>(OnUpdate);
        }
        
        private void OnUpdate(float delta)
        {
            if (!_active) return;
            _elapsedTime -= delta;

            if (_elapsedTime <= 0)
            {
                NotifyAll();
                _active = false;
            }
        }

        public void SetTimer(float duration)
        {
            _elapsedTime = duration;
            _active = true;
        }
        
        public bool Attach(IObserver observer, bool disposeOnDetach = false)
        {
            return _subscribers.Attach(observer, disposeOnDetach);
        }

        public bool Detach(IObserver observer)
        {
            return _subscribers.Detach(observer);
        }

        public void DetachAll()
        {
            _subscribers.DetachAll();
        }

        public void NotifyAll()
        {
            _subscribers.NotifyAll();
        }
        
        public bool TryGetUpdate(out DesignPatterns.Observers.IObserver<float> observer)
        {
            observer = _updateObserver;
            return _updateObserver != null;
        }

        public bool TryGetLateUpdate(out DesignPatterns.Observers.IObserver<float> observer)
        {
            observer = default;
            return false;
        }

        public bool TryGetFixedUpdate(out DesignPatterns.Observers.IObserver<float> observer)
        {
            observer = default;
            return false;
        }
        
        public void Dispose()
        {
            _updateObserver.Dispose();
            _updateObserver = null;
            
            _subscribers.Dispose();
            _subscribers = null;
        }

        public void OnDraw(Transform origin)
        {
            
        }

        public void OnDrawSelected(Transform origin)
        {
            
        }
    }
}
