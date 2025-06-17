using System;
using System.Collections.Generic;
using Game.DesignPatterns.Observers;

namespace Game.UI.Screens
{
    public abstract class UIState : IDisposable
    {
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
            
        }
    }
    
    public abstract class UIStatePresenter<TPresenter, TModel, TView> : UIState 
        where TModel : UIModel 
        where TView : UIView 
        where TPresenter : UIPresenter<TModel, TView> 
    {
        protected TPresenter Presenter;
        
        private ISubject _enableSubject = new Subject();
        private ISubject _disableSubject = new Subject();
        private ISubject _startSubject = new Subject();
        private ISubject _endSubject = new Subject();

        protected UIStatePresenter()
        {
            Init();
        }
        
        protected UIStatePresenter(TPresenter presenter) : this()
        {
            Presenter = presenter;
        }
        
        public virtual void Init()
        {
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
            Presenter.Dispose();
            
            _startSubject.Dispose();
            _startSubject = null;
            _endSubject.Dispose();
            _endSubject = null;
            _disableSubject.Dispose();
            _disableSubject = null;
            _enableSubject.Dispose();
            _enableSubject = null;
        }
    }
}