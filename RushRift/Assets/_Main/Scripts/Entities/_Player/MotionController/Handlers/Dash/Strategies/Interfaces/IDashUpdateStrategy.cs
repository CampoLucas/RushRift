using System;
using UnityEngine;

namespace Game.Entities.Components.MotionController.Strategies
{
    public interface IDashUpdateStrategy : IDisposable
    {
        void OnReset();
        /// <summary>
        /// Executes when the player dashes in the late update loop.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="delta"></param>
        /// <returns>Returns true to stop the dash</returns>
        bool OnUpdate(in MotionContext context, in float delta);
        /// <summary>
        /// Executes when the player dashes in the late update loop.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="delta"></param>
        /// <returns>Returns true to stop the dash</returns>
        bool OnLateUpdate(in MotionContext context, in float delta);
        /// <summary>
        /// Executes when the player collides while dashing.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="other"></param>
        /// <returns>Returns true to stop the dash.</returns>
        bool OnCollision(in MotionContext context, in Collider other);
    }
}