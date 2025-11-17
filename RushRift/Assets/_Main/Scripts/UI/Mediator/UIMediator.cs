using System;
using System.Collections.Generic;
using Game.UI.StateMachine;
using Game.UI.StateMachine.Interfaces;
using MyTools.Global;
using UnityEngine;

namespace Game.UI.Mediator
{
    public abstract class UIMediator : MonoBehaviour, DesignPatterns.Observers.IObserver<MenuState>
    {
        [Header("Settings")]
        [SerializeField] private UIScreen rootScreen;
        
        [Header("Presenters")]
        [SerializeField] private SerializedDictionary<UIScreen, BaseUIPresenter> presenters;

        [Header("Screen Transitions")]
        [SerializeField] private float fadeOut;
        [SerializeField] private float fadeIn;
        [SerializeField] private float fadeInStart;
        
        private UIStateMachine _stateMachine = new();
        private Dictionary<MenuState, Action> _actions = new();
        
        private void Start()
        {
            InitStateMachine(ref _stateMachine);
            InitActions(ref _actions);
        }

        private void Update()
        {
            _stateMachine.Update(Time.deltaTime);
        }

        protected abstract void InitActions(ref Dictionary<MenuState, Action> actions);

        protected void InitStateMachine(ref UIStateMachine stateMachine)
        {
            var keys = presenters.Keys;

            foreach (var key in keys)
            {
                var presenter = presenters[key];
                
                if (!presenter || !presenter.TryGetState(out var state)) continue;
                presenter.Attach(this);
                stateMachine.TryAddState(key, state);
            }

            _stateMachine.TransitionTo(rootScreen, 0, 0, 0);
        }

        protected void SetState(UIScreen screen)
        {
            _stateMachine.TransitionTo(screen, fadeOut, fadeIn, fadeInStart);
        }

        public void OnNotify(MenuState arg)
        {
            if (_actions.TryGetValue(arg, out var action))
            {
                action();
            }
        }

        public virtual void Dispose()
        {
            _stateMachine.Dispose();
            _actions.Clear();
            
            presenters.Dispose();
            presenters = null;
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}