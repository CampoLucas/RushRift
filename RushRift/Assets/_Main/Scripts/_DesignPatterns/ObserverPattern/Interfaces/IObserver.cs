using System;

namespace Game.DesignPatterns.Observers
{
    public interface IObserver : IDisposable
    {
        void OnNotify();
    }
    
    public interface IObserver<in T> : IDisposable
    {
        void OnNotify(T arg);
    }
    
    public interface IObserver<in T1, in T2> : IDisposable
    {
        void OnNotify(T1 arg1, T2 arg2);
    }
    
    public interface IObserver<in T1, in T2, in T3> : IDisposable
    {
        void OnNotify(T1 arg1, T2 arg2, T3 arg3);
    }
}


