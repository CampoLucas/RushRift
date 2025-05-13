using UnityEngine;

namespace Game.Entities
{
    public class EntityView<TData>: IView
        where TData : EntityViewSO
    {
        protected TData ViewData;
        protected Animator[] Animators;

        public EntityView(TData viewData)
        {
            ViewData = viewData;
        }

        public void Init(Animator[] animator)
        {
            Animators = animator;
        }

        public void Play(string name)
        {
            if (Animators.Length <= 0) return;

            for (var i = 0; i < Animators.Length; i++)
            {
                var anim = Animators[i];
                if (anim == null) continue;
                anim.Play(name);
            }
        }
        
        public void Play(string name, int layer, float normalizedTime = 0)
        {
            if (Animators.Length <= 0) return;

            for (var i = 0; i < Animators.Length; i++)
            {
                var anim = Animators[i];
                if (anim == null) continue;
                anim.Play(name, layer, normalizedTime);
            }
        }
        
        public void Dispose()
        {
            ViewData = null;
            Animators = null;
        }
    }
}