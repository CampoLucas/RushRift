using System;
using UnityEngine;

namespace Game.DesignPatterns.Observers
{
    public static class SubjectExtensions
    {
        public static ISubject Where<T1, T2, T3>(
            this ISubject<T1, T2, T3> source, 
            Func<T1, T2, T3, bool> predicate)
        {
            var newSubject = new Subject();

            source.Attach(new ActionObserver<T1, T2, T3>((arg1, arg2, arg3) =>
            {
                
                if (predicate(arg1, arg2, arg3))
                    newSubject.NotifyAll();
            }));

            return newSubject;
        }
        
        public static ISubject Where<T>(
            this ISubject<T> source, 
            Func<T, bool> predicate)
        {
            var newSubject = new Subject();
            var newObserver = new ActionObserver<T>(arg =>
            {
                Debug.Log("Where subject");
                if (predicate(arg))
                    newSubject.NotifyAll();
            });

            source.Attach(newObserver);

            return new DisposableSubject(newSubject, () => DisposeObserver(newObserver, source));
        }

        private static void DisposeObserver<T>(IObserver<T> observer, ISubject<T> source)
        {
            if (source != null)
            {
                source.Detach(observer);
            }
            
            observer?.Dispose();
        }
    }
}