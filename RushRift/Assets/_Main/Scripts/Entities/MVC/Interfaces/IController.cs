using System;
using System.Collections;
using UnityEngine;

namespace Game.Entities
{
    public interface IController : IDisposable
    {
        Transform Transform { get; }
        Transform EyesTransform { get; }
        Transform SpawnPos { get; }
        IModel GetModel();
        IView GetView();

        Vector3 MoveDirection();

        Coroutine DoCoroutine(IEnumerator routine);
        void EndCoroutine(Coroutine coroutine);
        void EndAllCoroutines();
    }
}