using System;
using System.Collections.Generic;
using Game.DesignPatterns.Observers;
using MyTools.Global;
using UnityEngine;

namespace Game.UI.StateMachine
{
    public abstract class UIState : IDisposable
    {
        public HashSet<UITransition> Transitions { get; private set; } = new();
        protected NullCheck<ActionObserver<bool>> LoadingObserver;

        public virtual void Init()
        {
            if (LoadingObserver.TryGet(out var observer))
            {
                GameEntry.LoadingState.AttachOnLoading(observer);
            }
        }
        
        public virtual void Enable()
        {
            // show ui
        }

        public virtual void Disable()
        {
            // hide ui
        }

        public virtual void Start()
        {
            // enable interactions
        }

        public virtual void End()
        {
            // disable interactions
        }
        
        public virtual void FadeIn(float t, float startTime, float duration)
        {
            // fade in effect
        }

        public virtual void FadeOut(float t, float startTime, float duration)
        {
            // fade out effect
        }

        public virtual void Dispose()
        {
            if (LoadingObserver.TryGet(out var observer))
            {
                GameEntry.LoadingState.DetachOnLoading(observer);
            }
            
            if (Transitions != null)
            {
                foreach (var transition in Transitions)
                {
                    transition.Dispose();
                }
            
                Transitions.Clear();
                Transitions = null;
            }
        }

        public void AddTransition(UIScreen to, IPredicate condition, float fadeOut = 0f, float fadeIn = 0f, float fadeInStart = 0f)
        {
            Transitions.Add(new UIScreenTransition(to, condition, fadeOut, fadeIn, fadeInStart));
        }

        public void AddTransition(SceneTransition sceneTransition, IPredicate condition)
        {
            Transitions.Add(new UISceneTransition(sceneTransition, condition));
        }

        public void AddTransition(string sceneName, IPredicate condition)
        {
            Transitions.Add(new UISceneTransition(sceneName, condition));
        }

        protected virtual void OnInit()
        {
            
        }
    }
    
    public abstract class UIState<TPresenter, TModel, TView> : UIState 
        where TModel : UIModel 
        where TView : UIView 
        where TPresenter : UIPresenter<TModel, TView> 
    {
        protected TPresenter Presenter;
        
        private ISubject _enableSubject = new Subject();
        private ISubject _disableSubject = new Subject();
        private ISubject _startSubject = new Subject();
        private ISubject _endSubject = new Subject();

        private UIState()
        {
            Init();
        }
        
        protected UIState(TPresenter presenter) : this()
        {
            Presenter = presenter;
        }
        
        public sealed override void Init()
        {
            base.Init();
            
            _enableSubject.Attach(new ActionObserver(Enable));
            _disableSubject.Attach(new ActionObserver(Disable));
            _startSubject.Attach(new ActionObserver(Start));
            _endSubject.Attach(new ActionObserver(End));
        }
        
        public override void Enable()
        {
            Presenter.Begin();
        }

        public override void Disable()
        {
            Presenter.End();
        }

        public override void FadeIn(float t, float startTime, float duration)
        {
            Presenter.FadeIn(t, startTime, duration, ref _enableSubject, ref _startSubject);
        }

        public override void FadeOut(float t, float startTime, float duration)
        {
            Presenter.FadeOut(t, startTime, duration, ref _endSubject, ref _disableSubject);
        }

        public override void Dispose()
        {
            base.Dispose();
            
            _startSubject?.Dispose();
            _startSubject = null;
            _endSubject?.Dispose();
            _endSubject = null;
            _disableSubject?.Dispose();
            _disableSubject = null;
            _enableSubject?.Dispose();
            _enableSubject = null;
        }
    }
}