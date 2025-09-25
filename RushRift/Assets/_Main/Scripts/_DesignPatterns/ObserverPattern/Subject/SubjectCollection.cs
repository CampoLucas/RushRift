using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

namespace Game.DesignPatterns.Observers
{
    public class SubjectCollection<TSubject> : ISubject, IObserver, ICollection<TSubject>
        where TSubject : ISubject
    {
        public int Count => Subjects.Count;
        public int SubscriberCount => _subscribers.Count;
        public bool IsReadOnly { get; private set; }
        
        protected readonly HashSet<TSubject> Subjects = new();
        private readonly HashSet<IObserver> _subscribers = new();
        
        private readonly bool _detachOnNotify;
        private readonly bool _disposeOnDetach;
        private readonly bool _disposeOnClear;
        
        public SubjectCollection(bool detachOnNotify = false, bool disposeOnDetach = false, bool disposeOnClear = false, bool isReadOnly = false)
        {
            _detachOnNotify = detachOnNotify;
            _disposeOnDetach = disposeOnDetach;
            _disposeOnClear = disposeOnClear;
            IsReadOnly = isReadOnly;
        }

        public bool Attach(IObserver observer) => observer != null && _subscribers.Add(observer);
        public bool Detach(IObserver observer)
        {
            if (_subscribers.Remove(observer))
            {
                if (_disposeOnDetach) observer.Dispose();
                return true;
            }

            return false;
        }

        public void DetachAll()
        {
            if (_disposeOnDetach)
            {
                foreach (var subscriber in _subscribers)
                {
                    subscriber.Dispose();
                }
            }
            
            _subscribers.Clear();
        }

        public void NotifyAll()
        {
            var subscribers = _subscribers.ToList();
        
            for (var i = 0; i < subscribers.Count; i++)
            {
                var subscriber = subscribers[i];
                if (subscriber == null)
                {
                    continue;
                }
                subscriber.OnNotify();
        
                if (_detachOnNotify) Detach(subscriber);
            }
        }
        
        public void OnNotify()
        {
            NotifyAll();
        }

        public IEnumerator<TSubject> GetEnumerator()
        {
            return Subjects.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(TSubject item)
        {
            if (item != null && Subjects.Add(item))
            {
                item.Attach(this);
            }
        }

        public void AddRange(IEnumerable<TSubject> range)
        {
            foreach (var item in range)
            {
                Add(item);
            }
        }
        
        public bool Remove(TSubject item)
        {
            if (item != null && Subjects.Remove(item))
            {
                item.Detach(this);
                return true;
            }

            return false;
        }

        public void Clear()
        {
            foreach (var subject in Subjects)
            {
                subject.Detach(this);
                if (_disposeOnClear) subject.Dispose();
            }
            
            Subjects.Clear();
        }

        public bool Contains(TSubject item)
        {
            return Subjects.Contains(item);
        }

        public void CopyTo(TSubject[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Index must be non-negative.");
            if (array.Length - arrayIndex < Subjects.Count)
                throw new ArgumentException("The target array is too small to copy the elements.");

            foreach (var item in Subjects)
            {
                array[arrayIndex++] = item;
            }
        }
        
        public void Dispose()
        {
            Clear();
            DetachAll();
        }
    }
}