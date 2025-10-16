using Game.DesignPatterns.Observers;
using UnityEngine;

namespace Game.UI.Screens.Interfaces
{
    public abstract class BaseUIPresenter : MonoBehaviour, ISubject<MenuState>
    {
        private NullCheck<Subject<MenuState>> _subject = new Subject<MenuState>();
        
        public abstract bool TryGetState(out UIState state);
        
        public bool Attach(DesignPatterns.Observers.IObserver<MenuState> observer)
        {
            return _subject.TryGetValue(out var subject) && subject.Attach(observer);
        }

        public bool Detach(DesignPatterns.Observers.IObserver<MenuState> observer)
        {
            return _subject.TryGetValue(out var subject) && subject.Detach(observer);
        }

        public void DetachAll()
        {
            if (_subject.TryGetValue(out var subject))
            {
                subject.DetachAll();
            }
        }

        public void NotifyAll(MenuState arg)
        {
            if (_subject.TryGetValue(out var subject))
            {
                subject.NotifyAll(arg);
            }
        }
        
        public virtual void Dispose()
        {
            if (_subject.TryGetValue(out var subject))
            {
                subject.DetachAll();
                subject.Dispose();
                _subject = null;
            }
        }

    }
}