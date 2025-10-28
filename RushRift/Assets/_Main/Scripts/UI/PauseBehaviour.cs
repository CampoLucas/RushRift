using Game.DesignPatterns.Observers;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.UI
{
    public abstract class PauseBehaviour : MonoBehaviour
    {
        protected bool Paused { get => _paused; private set => _paused = value; }
        
        private ActionObserver<bool> _onPause;
        private bool _paused;

        protected virtual void Awake()
        {
            _onPause = new ActionObserver<bool>(OnPauseHandler);
            if (PauseHandler.IsPaused)
            {
                OnPauseHandler(true);
            }
        }

        protected virtual void Start()
        {
            PauseHandler.Attach(_onPause);
        }

        private void OnPauseHandler(bool paused)
        {
            _paused = paused;

            if (paused)
            {
                OnPause();
            }
            else
            {
                OnUnpause();
            }
        }
        
        protected abstract void OnPause();
        protected abstract void OnUnpause();

        protected virtual void OnDestroy()
        {
            PauseHandler.Detach(_onPause);
            _onPause?.Dispose();
        }
    }
}