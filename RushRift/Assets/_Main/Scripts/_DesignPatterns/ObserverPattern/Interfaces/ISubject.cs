using System;

namespace Game.DesignPatterns.Observers
{
    public interface ISubject : IDisposable
    {
        bool Attach(IObserver observer, bool disposeOnDetach = false);
        bool Detach(IObserver observer);
        void DetachAll();
        void NotifyAll();
    }
    
    public interface ISubject<T> : IDisposable
    {
        bool Attach(IObserver<T> observer, bool disposeOnDetach = false);
        bool Detach(IObserver<T> observer);
        void DetachAll();
        void NotifyAll(T arg);
    }
    
    public interface ISubject<T1, T2> : IDisposable
    {
        bool Attach(IObserver<T1, T2> observer, bool disposeOnDetach = false);
        bool Detach(IObserver<T1, T2> observer);
        void DetachAll();
        void NotifyAll(T1 arg1, T2 arg2);
    }
    
    public interface ISubject<T1, T2, T3> : IDisposable
    {
        bool Attach(IObserver<T1, T2, T3> observer, bool disposeOnDetach = false);
        bool Detach(IObserver<T1, T2, T3> observer);
        void DetachAll();
        void NotifyAll(T1 arg1, T2 arg2, T3 arg3);
    }
}