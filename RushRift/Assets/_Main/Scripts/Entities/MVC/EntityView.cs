using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;

namespace Game.Entities
{
    /// <summary>
    /// Generic runtime view class created from a ScriptableObject. Handles animation logic
    /// </summary>
    /// <typeparam name="TData">The type of ViewSO</typeparam>
    public class EntityView : MonoBehaviour, IView
    {
        protected Animator[] Animators; // Array of Animator components used to play animations

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

        public void Play(int id)
        {
            if (Animators.Length <= 0) return;

            if (Animators.Length == 1)
            {
                var anim = Animators[0];
                if (anim == null) return;
                anim.Play(id);
            }
            else
            {
                for (var i = 0; i < Animators.Length; i++)
                {
                    var anim = Animators[i];
                    if (anim == null) continue;
                    anim.Play(id);
                }
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

        public void SetFloat(int id, float value)
        {
            if (Animators.Length <= 0) return;

            if (Animators.Length == 1)
            {
                var anim = Animators[0];
                if (anim == null) return;
                anim.SetFloat(id, value);
            }
            else
            {
                for (var i = 0; i < Animators.Length; i++)
                {
                    var anim = Animators[i];
                    if (anim == null) continue;
                    anim.SetFloat(id, value);
                }
            }
        }

        public void SetBool(int id, bool value)
        {
            if (Animators.Length <= 0) return;

            if (Animators.Length == 1)
            {
                var anim = Animators[0];
                if (anim == null) return;
                anim.SetBool(id, value);
            }
            else
            {
                for (var i = 0; i < Animators.Length; i++)
                {
                    var anim = Animators[i];
                    if (anim == null) continue;
                    anim.SetBool(id, value);
                }
            }
        }
        
        /// <summary>
        /// Cleans up references
        /// </summary>
        public void Dispose()
        {
            OnDispose();
            Animators = null;
        }
        
        protected virtual void OnDispose() { }
        
        private void OnDestroy()
        {
            Dispose();
        }
    }
}