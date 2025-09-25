using System;

namespace Game.DesignPatterns.Observers
{
    public class ActionObserverBase<T> : IDisposable
    {
        protected NullCheck<T> Action;

        protected ActionObserverBase(T action)
        {
            Action.Set(action);
        }

        public void Dispose()
        {
            Action.Dispose();
        }
    }
    
    public class ActionObserver : ActionObserverBase<Action>, IObserver
    {
        public ActionObserver(Action action) : base(action)
        {
        }
        
        public void OnNotify()
        {
            if (Action.TryGetValue(out var action)) action();
        }
    }
    
    public class ActionObserver<T> : ActionObserverBase<Action<T>>, IObserver<T>
    {
        public ActionObserver(Action<T> action) : base(action)
        {
        }
        
        public void OnNotify(T arg)
        {
            if (Action.TryGetValue(out var action)) action(arg);
        }
    }
    
    public class ActionObserver<T1, T2> : ActionObserverBase<Action<T1, T2>>, IObserver<T1, T2>
    {
        public ActionObserver(Action<T1, T2> action) : base(action)
        {
        }
        
        public void OnNotify(T1 arg1, T2 arg2)
        {
            if (Action.TryGetValue(out var action)) action(arg1, arg2);
        }
    }
    
    public class ActionObserver<T1, T2, T3> : ActionObserverBase<Action<T1, T2, T3>>, IObserver<T1, T2, T3>
    {
        public ActionObserver(Action<T1, T2, T3> action) : base(action)
        {
        }
        
        public void OnNotify(T1 arg1, T2 arg2, T3 arg3)
        {
            if (Action.TryGetValue(out var action)) action(arg1, arg2, arg3);
        }
    }
    
    // public class ActionObserver<T> : IObserver<T>
    // {
    //     private NullCheck<Action<T>> _action;
    //
    //     public ActionObserver(Action<T> action)
    //     {
    //         _action.Set(action);
    //     }
    //     
    //     public void OnNotify(T arg)
    //     {
    //         if (_action.TryGetValue(out var action)) action(arg);
    //     }
    //     
    //     public void Dispose()
    //     {
    //         _action.Dispose();
    //     }
    // }
}