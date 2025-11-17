using System;
using Game.DesignPatterns.Observers;
using UnityEngine;

namespace Game.LevelElements.SubjectTrigger
{
    public class SubjectTrigger : MonoBehaviour, ISubject<string>
    {
        [Header("Settings")]
        [SerializeField] private string argument = "on";
        [SerializeField] private string targetTag = "Player";
        [SerializeField] private bool onEnter = true;
        
        [Header("Observers")]
        [SerializeField] private ObserverComponent[] observers;

        private ISubject<string> _subject = new Subject<string>();
        private bool _used;

        private void Awake()
        {
            if (observers != null)
            {
                for (var i = 0; i < observers.Length; i++)
                {
                    var o = observers[i];
                    if (o) _subject.Attach(o);
                }
            }
            
            Reset();
        }

        public bool Attach(DesignPatterns.Observers.IObserver<string> observer, bool disposeOnDetach = false) => _subject.Attach(observer, disposeOnDetach);
        public bool Detach(DesignPatterns.Observers.IObserver<string> observer) => _subject.Detach(observer);
        public void DetachAll() => _subject.DetachAll();
        public void NotifyAll(string arg) => _subject.NotifyAll(arg);

        public void Reset()
        {
            _used = false;
        }
        
        public void Dispose()
        {
            observers = null;
            _subject.DetachAll();
            _subject.Dispose();
            _subject = null;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!onEnter) return;
            OnTrigger(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (onEnter) return;
            OnTrigger(other);
        }

        private void OnTrigger(Collider other)
        {
            if (_used || !other.gameObject.CompareTag(targetTag)) return;
            _used = true;
            
            NotifyAll(argument);
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}