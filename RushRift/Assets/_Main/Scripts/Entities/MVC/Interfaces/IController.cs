using System;
using System.Collections;
using Game.DesignPatterns.Observers;
using UnityEngine;
using UnityEngine.VFX;

namespace Game.Entities
{
    /// <summary>
    /// Interface used to abstract interactions with any type of controller,
    /// allowing systems (AI, animations, input, etc.) to work without knowing the specific controller implementation.
    /// </summary>
    public interface IController : IDisposable, DesignPatterns.Observers.IObserver<string>
    {
        /// <summary>
        /// World origin point for the entity (usually its transform)
        /// </summary>
        Transform Origin { get; }
        /// <summary>
        /// Collections of transforms, useful for making custom attacks that use different bone joints
        /// </summary>
        Joints<EntityJoint> Joints { get; }
        /// <summary>
        /// Returns the model instance associated with this controller
        /// </summary>
        /// <returns>A model instance</returns>
        IModel GetModel();
        /// <summary>
        /// Returns the view instance associated with this controller
        /// </summary>
        /// <returns>A view instance</returns>
        IView GetView();
        /// <summary>
        /// Movement direction based on the controller
        /// </summary>
        /// <returns></returns>
        Vector3 MoveDirection();

        bool TryGetObserver(string key, out IObserver observer);
        bool TryGetSubject(string key, out ISubject subject);
    }
}