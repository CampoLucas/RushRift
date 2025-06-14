using System;
using Game.DesignPatterns.Observers;

namespace Game.UI.Screens
{
    public class UIPresenter<TModel, TView> : IDisposable
        where TModel : UIModel 
        where TView : UIView
    {
        protected TModel Model;
        protected TView View;

        public UIPresenter(TModel model, TView view)
        {
            Model = model;
            View = view;
        }
        
        public virtual void Begin()
        {
            View.Show();
        }

        public virtual void End()
        {
            View.Hide();   
        }

        public void FadeIn(float t, float startTime, float duration, ref ISubject onStart, ref ISubject onEnd)
        {
            View.FadeIn(t, startTime, duration, ref onStart, ref onEnd);
        }

        public void FadeOut(float t, float startTime, float duration, ref ISubject onStart, ref ISubject onEnd)
        {
            View.FadeOut(t, startTime, duration, ref onStart, ref onEnd);
        }

        public virtual void Dispose()
        {
            Model.Dispose();
            Model = null;
            
            View.Dispose();
            View = null;
        }
    }
}