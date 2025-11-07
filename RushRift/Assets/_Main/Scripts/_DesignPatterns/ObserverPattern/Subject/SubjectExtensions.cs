using System;
using UnityEngine;

namespace Game.DesignPatterns.Observers
{
    // ToDo: Test if in a chain of .Where all created observers and subjects are disposed
    // The Subject class wouldn't dispose correctly if used in chain 
    public static class SubjectExtensions 
    {
        public static ISubject Where<T1, T2, T3>(
            this ISubject<T1, T2, T3> source, 
            Func<T1, T2, T3, bool> predicate)
        {
            var newSubject = new Subject();
            var newObserver = new ActionObserver<T1, T2, T3>((arg1, arg2 ,arg3) =>
            {
                if (predicate(arg1, arg2, arg3))
                    newSubject.NotifyAll();
            });

            source.Attach(newObserver, true);

            return new DisposableSubject(newSubject, () => DisposeObserver(newObserver, source));
        }
        
        public static ISubject Where<T>(
            this ISubject<T> source, 
            Func<T, bool> predicate)
        {
            var newSubject = new Subject();
            var newObserver = new ActionObserver<T>(arg =>
            {
                if (predicate(arg))
                    newSubject.NotifyAll();
            });

            source.Attach(newObserver, true);

            return new DisposableSubject(newSubject, () => DisposeObserver(newObserver, source));
        }

        public static ISubject ConvertToSimple<T1, T2, T3>(this ISubject<T1, T2, T3> source)
        {
            var newSubject = new Subject();
            var newObserver = new ActionObserver<T1, T2, T3>((a, b, c) =>
            {
                newSubject.NotifyAll();
            });

            source.Attach(newObserver, true);
            
            return new DisposableSubject(newSubject, () => DisposeObserver(newObserver, source));
        }
        
        public static ISubject ConvertToSimple<T1, T2>(this ISubject<T1, T2> source)
        {
            var newSubject = new Subject();
            var newObserver = new ActionObserver<T1, T2>((a, b) =>
            {
                newSubject.NotifyAll();
            });

            source.Attach(newObserver, true);
            
            return new DisposableSubject(newSubject, () => DisposeObserver(newObserver, source));
        }
        
        public static ISubject ConvertToSimple<T>(this ISubject<T> source)
        {
            var newSubject = new Subject();
            var newObserver = new ActionObserver<T>((a) =>
            {
                newSubject.NotifyAll();
            });

            source.Attach(newObserver, true);
            
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
        
        private static void DisposeObserver<T1, T2>(IObserver<T1, T2> observer, ISubject<T1, T2> source)
        {
            if (source != null)
            {
                source.Detach(observer);
            }
            
            observer?.Dispose();
        }
        
        private static void DisposeObserver<T1, T2, T3>(IObserver<T1, T2, T3> observer, ISubject<T1, T2, T3> source)
        {
            if (source != null)
            {
                source.Detach(observer);
            }
            
            observer?.Dispose();
        }
    }
}