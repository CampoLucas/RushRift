using System;
using Game.DesignPatterns.Observers;
using UnityEngine;

namespace Game.Entities
{
    public class Trigger : ISubject, IPredicate<IController>
    {
        private ISubject _subject;
        private IPredicate<IController> _predicate;

        public Trigger(ISubject subject, IPredicate<IController> predicate)
        {
            _subject = subject;
            _predicate = predicate;
        }
        
        public bool Evaluate(ref IController args) => _predicate.Evaluate(ref args);
        public bool Attach(IObserver observer) => _subject.Attach(observer);
        public bool Detach(IObserver observer) => _subject.Detach(observer);
        public void DetachAll() => _subject.DetachAll();

        public void NotifyAll()
        {
#if UNITY_EDITOR
            Debug.LogWarning("WARNING: The Trigger Subject cannot notify the observers");
#endif
        }
        
        public void Dispose()
        {
            _subject.Dispose();
            _subject = null;
            
            _predicate.Dispose();
            _predicate = null;
        }
    }
}