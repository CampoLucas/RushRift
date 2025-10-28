using System;

namespace Game.DesignPatterns.Observers
{
    public class SubjectObserver : Subject, IObserver
    {
        public void OnNotify()
        {
            NotifyAll();
        }
    }

    public class SubjectObserver<T> : Subject<T>, IObserver<T>
    {
        public void OnNotify(T arg)
        {
            NotifyAll(arg);
        }
    }

    public class SubjectObserver<T1, T2> : Subject<T1, T2>, IObserver<T1, T2>
    {
        public void OnNotify(T1 arg1, T2 arg2)
        {
            NotifyAll(arg1, arg2);
        }
    }

    public class SubjectObserver<T1, T2, T3> : Subject<T1, T2, T3>, IObserver<T1, T2, T3>
    {
        public void OnNotify(T1 arg1, T2 arg2, T3 arg3)
        {
            NotifyAll(arg1, arg2, arg3);
        }
    }
}