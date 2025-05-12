using System;
using System.Collections;
using System.Collections.Generic;
using Game.DesignPatterns.Observers;
using UnityEngine;

namespace Game.Entities.Components
{
    public class TimeElapsedSubject : IEntityComponent, ISubject
    {
        private HashSet<IObserver> _subscribers;
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
        
        public bool Attach(IObserver observer)
        {
            return _subscribers.Add(observer);
        }

        public bool Detach(IObserver observer)
        {
            return _subscribers.Remove(observer);
        }

        public void DetachAll()
        {
            _subscribers.Clear();
        }

        public void NotifyAll()
        {
            foreach (var subscriber in _subscribers)
            {
                subscriber.OnNotify();
            }
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
            
            _subscribers.Clear();
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
