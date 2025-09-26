

using System;

namespace Game.DesignPatterns.Observers
{
    public class DisposableSubject : ISubject
    {
        private ISubject _inner;
        private Action _onDispose;
        private bool _disposed;
        
        public DisposableSubject(ISubject inner, Action onDispose)
        {
            _inner = inner;
            _onDispose = onDispose;
        }
        
        public bool Attach(IObserver observer) => _inner.Attach(observer);
        public bool Detach(IObserver observer) => _inner.Detach(observer);
        public void NotifyAll() => _inner.NotifyAll();
        public void DetachAll() => _inner.DetachAll();

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            
            _onDispose?.Invoke();
            _onDispose = null;
            
            _inner?.Dispose();
            _inner = null;
        }
    }
}