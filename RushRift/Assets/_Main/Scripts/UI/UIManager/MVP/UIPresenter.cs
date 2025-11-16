using System;
using Game.DesignPatterns.Observers;
using Game.UI.StateMachine.Interfaces;
using UnityEngine;

namespace Game.UI.StateMachine
{
    public abstract class UIPresenter<TModel, TView> : BaseUIPresenter, IDisposable
        where TModel : UIModel 
        where TView : UIView
    {
        [SerializeField] protected TView View;
        protected TModel Model;
        

        public void Init(TModel model)
        {
            Model = model;
            OnInit();
        }
        
        public override void Begin()
        {
            View.Show();
        }

        public override void End()
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
        
        public TModel GetModel()
        {
            return Model;
        }

        public override void Dispose()
        {
            base.Dispose();
            
            Model?.Dispose();
            Model = null;
            View = null;
        }

        protected virtual void OnInit()
        {
            
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}