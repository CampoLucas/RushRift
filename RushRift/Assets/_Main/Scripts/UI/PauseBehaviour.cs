using Game.DesignPatterns.Observers;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.UI
{
    public abstract class PauseBehaviour : MonoBehaviour
    {
        protected bool Paused { get => _paused; private set => _paused = value; }
        
        private ActionObserver _onPause;
        private ActionObserver _onUnpause;
        private bool _paused;

        protected virtual void Awake()
        {
            _onPause = new ActionObserver(OnPauseHandler);
            _onUnpause = new ActionObserver(OnUnpauseHandler);
        }

        protected virtual void Start()
        {
            UIManager.OnPaused.Attach(_onPause);
            UIManager.OnUnpaused.Attach(_onUnpause);
        }

        private void OnPauseHandler()
        {
            _paused = true;
            OnPause();
        }
        
        private void OnUnpauseHandler()
        {
            _paused = false;
            OnUnpause();
        }
        
        protected abstract void OnPause();
        protected abstract void OnUnpause();

        protected virtual void OnDestroy()
        {
            UIManager.OnPaused.Detach(_onPause);
            UIManager.OnUnpaused.Detach(_onUnpause);
            
            _onPause.Dispose();
            _onUnpause.Dispose();
        }
    }
}