using System;
using UnityEngine;

namespace Game.Entities
{
    public interface IController : IDisposable
    {
        Transform Transform { get; }
        Transform EyesTransform { get; }
        IModel GetModel();
        IView GetView();

        Vector3 MoveDirection();
    }
}