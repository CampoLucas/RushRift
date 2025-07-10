using UnityEngine;

namespace Game.Entities
{
    /// <summary>
    /// Generic runtime view class created from a ScriptableObject. Handles animation logic
    /// </summary>
    /// <typeparam name="TData">The type of ViewSO</typeparam>
    public class EntityView<TData>: IView
        where TData : EntityViewSO
    {
        protected TData ViewData; // Reference to the view data (SO)
        protected Animator[] Animators; // Array of Animator components used to play animations

        public EntityView(TData viewData)
        {
            ViewData = viewData;
        }

        /// <summary>
        /// Initializes the view with an array of Animators
        /// </summary>
        /// <param name="animator">Animators to use</param>
        public void Init(Animator[] animator)
        {
            Animators = animator;
        }

        /// <summary>
        /// Plays the specified animation on all animators
        /// </summary>
        /// <param name="name">Name of the animation state</param>
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
        
        /// <summary>
        /// Plays the specified animation on all animators on a given layer and normalized time
        /// </summary>
        /// <param name="name">Name of the animation state.</param>
        /// <param name="layer">Animator layer index</param>
        /// <param name="normalizedTime">Normalized time to start the animation at</param>
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
        
        /// <summary>
        /// Cleans up references
        /// </summary>
        public void Dispose()
        {
            ViewData = null;
            Animators = null;
        }
    }
}