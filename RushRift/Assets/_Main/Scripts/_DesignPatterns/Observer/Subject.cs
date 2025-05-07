using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.DesignPatterns.Observers
{
    public class BaseSubject<TObserver> : IDisposable
        where TObserver : IDisposable
    {
        protected HashSet<TObserver> Subscribers = new();
        protected readonly bool _detachOnNotify;
        protected readonly bool _disposeOnDetach;
        
        protected BaseSubject(bool detachOnNotify = false, bool disposeOnDetach = false)
        {
            _detachOnNotify = detachOnNotify;
            _disposeOnDetach = disposeOnDetach;
        }
        
        public bool Attach(TObserver observer) => observer != null && Subscribers.Add(observer);

        public bool Detach(TObserver observer)
        {
            if (Subscribers.Remove(observer))
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
                foreach (var subscriber in Subscribers)
                {
                    subscriber.Dispose();
                }
            }
            
            Subscribers.Clear();
        }

        public void Dispose()
        {
            DetachAll();
            Subscribers = null;
        }
    }

    public class Subject : BaseSubject<IObserver>, ISubject
    {
        public Subject(bool detachOnNotify = false, bool disposeOnDetach = false) : base(detachOnNotify, disposeOnDetach)
        {
            
        }
        
        public void NotifyAll()
        {
            var subscribers = Subscribers.ToList();
        
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
    }

    public class Subject<T> : BaseSubject<IObserver<T>>, ISubject<T>
    {
        public Subject(bool detachOnNotify = false, bool disposeOnDetach = false) : base(detachOnNotify, disposeOnDetach)
        {
            
        }
        
        public void NotifyAll(T arg)
        {
            var subscribers = Subscribers.ToList();
        
            for (var i = 0; i < subscribers.Count; i++)
            {
                var subscriber = subscribers[i];
                if (subscriber == null)
                {
                    continue;
                }
                subscriber.OnNotify(arg);
        
                if (_detachOnNotify) Detach(subscriber);
            }
        }
    }

    public class Subject<T1, T2> : BaseSubject<IObserver<T1, T2>>, ISubject<T1, T2>
    {
        public Subject(bool detachOnNotify = false, bool disposeOnDetach = false) : base(detachOnNotify, disposeOnDetach)
        {
            
        }
        
        public void NotifyAll(T1 arg1, T2 arg2)
        {
            var subscribers = Subscribers.ToList();
        
            for (var i = 0; i < subscribers.Count; i++)
            {
                var subscriber = subscribers[i];
                if (subscriber == null)
                {
                    continue;
                }
                subscriber.OnNotify(arg1, arg2);
        
                if (_detachOnNotify) Detach(subscriber);
            }
        }
    }

    public class Subject<T1, T2, T3> : BaseSubject<IObserver<T1, T2, T3>>, ISubject<T1, T2, T3>
    {
        public void NotifyAll(T1 arg1, T2 arg2, T3 arg3)
        {
            var subscribers = Subscribers.ToList();
        
            for (var i = 0; i < subscribers.Count; i++)
            {
                var subscriber = subscribers[i];
                if (subscriber == null)
                {
                    continue;
                }
                subscriber.OnNotify(arg1, arg2, arg3);
        
                if (_detachOnNotify) Detach(subscriber);
            }
        }
    }
    
    
    
    // public class Subject : ISubject
    // {
    //     private HashSet<IObserver> _subscribers = new();
    //     private bool _detachOnNotify;
    //     private bool _disposeOnDetach;
    //     
    //     public Subject(bool detachOnNotify = false, bool disposeOnDetach = false)
    //     {
    //         _detachOnNotify = detachOnNotify;
    //         _disposeOnDetach = disposeOnDetach;
    //     }
    //     
    //     public bool Attach(IObserver observer) => observer != null && _subscribers.Add(observer);
    //
    //     public bool Detach(IObserver observer)
    //     {
    //         if (_subscribers.Remove(observer))
    //         {
    //             if (_disposeOnDetach) observer.Dispose();
    //             return true;
    //         }
    //
    //         return false;
    //     }
    //     public void DetachAll() => _subscribers.Clear();
    //
    //     public void NotifyAll()
    //     {
    //         var subscribers = _subscribers.ToList();
    //
    //         for (var i = 0; i < subscribers.Count; i++)
    //         {
    //             var subscriber = subscribers[i];
    //             if (subscriber == null)
    //             {
    //                 continue;
    //             }
    //             subscriber.OnNotify();
    //
    //             if (_detachOnNotify) Detach(subscriber);
    //         }
    //     }
    //     
    //     public void Dispose()
    //     {
    //         _subscribers.Clear();
    //         _subscribers = null;
    //     }
    // }
    //
    // public class Subject<T> : ISubject<T>
    // {
    //     private HashSet<IObserver<T>> _subscribers = new();
    //
    //     public bool Attach(IObserver<T> observer) => _subscribers.Add(observer);
    //     public bool Detach(IObserver<T> observer) => _subscribers.Remove(observer);
    //     public void DetachAll() => _subscribers.Clear();
    //
    //     public void NotifyAll(T arg)
    //     {
    //         foreach (var subscriber in _subscribers)
    //         {
    //             subscriber.OnNotify(arg);
    //         }
    //     }
    //     
    //     public void Dispose()
    //     {
    //         _subscribers.Clear();
    //         _subscribers = null;
    //     }
    // }
}