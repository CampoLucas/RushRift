using Game.DesignPatterns.Observers;
using UnityEngine;

namespace Game
{
    public static class PauseHandler
    {
        public static bool IsPaused { get; private set; }
        private static readonly NullCheck<Subject<bool>> _gamePaused = new Subject<bool>();

        public static bool Attach<TObserver>(TObserver observer) where TObserver : IObserver<bool>
        {
            if (observer == null)
            {
                Debug.LogError("ERROR: Trying to attach a null observer to the PauseHandler.");
                return false;
            }

            if (!_gamePaused)
            {
                Debug.LogError("ERROR: Trying to attach an observer to a null _gamePause subject.");
                return false;
            }

            _gamePaused.Get().Attach(observer);
            return true;
        }
        
        public static bool Detach<TObserver>(TObserver observer) where TObserver : IObserver<bool>
        {
            if (observer == null)
            {
                Debug.LogError("ERROR: Trying to detach a null observer to the PauseHandler");
                return false;
            }

            if (!_gamePaused)
            {
                Debug.LogError("ERROR: Trying to detach an observer to a null _gamePause subject.");
                return false;
            }

            _gamePaused.Get().Detach(observer);
            return true;
        }
        
        public static void Pause(bool pause)
        {
            if (pause == IsPaused) return;
            IsPaused = pause;

            if (_gamePaused.TryGetValue(out var subject))
            {
                subject.NotifyAll(pause);
            }
        }

        public static void TogglePause()
        {
            Pause(!IsPaused);
        }

        public static void Dispose()
        {
            if (_gamePaused.TryGetValue(out var subject))
            {
                subject.DetachAll();
            }
        }
    }
}