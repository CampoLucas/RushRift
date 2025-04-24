using UnityEngine;

namespace Game.Entities
{
    public class EntityView<TData>: IView
        where TData : EntityViewSO
    {
        protected TData ViewData;
        protected Animator Animator;

        public EntityView(TData viewData)
        {
            ViewData = viewData;
        }

        public void Init(Animator animator)
        {
            Animator = animator;
        }

        public void Play(string name)
        {
            Animator.Play(name);
        }
        
        public void Dispose()
        {
            ViewData = null;
            Animator = null;
        }
    }
}