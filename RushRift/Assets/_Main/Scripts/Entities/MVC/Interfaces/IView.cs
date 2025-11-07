using System;
using UnityEngine;

namespace Game.Entities
{
    /// <summary>
    /// Interface for the runtime view instances
    /// </summary>
    public interface IView : IDisposable
    {
        /// <summary>
        /// Initializes the view with an array of Animator components
        /// </summary>
        /// <param name="animator">Animators to use</param>
        void Init(Animator[] animator);
        /// <summary>
        /// Plays the specified animation on all animators
        /// </summary>
        /// <param name="name">Animation state</param>
        void Play(string name);
        /// <summary>
        /// Plays the specified animation on all animators on a given layer and normalized time
        /// </summary>
        /// <param name="name">Animation state</param>
        /// <param name="layer">Layer index</param>
        /// <param name="time">Normalized time to start</param>
        void Play(string name, int layer, float time = 0);
    }
}