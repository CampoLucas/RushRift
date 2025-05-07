using System;
using System.Collections;
using UnityEngine;

namespace Game.Entities
{
    public interface IController : IDisposable
    {
        Transform Origin { get; }
        Joints<EntityJoint> Joints { get; }
        IModel GetModel();
        IView GetView();

        Vector3 MoveDirection();

        Coroutine DoCoroutine(IEnumerator routine);
        void EndCoroutine(Coroutine coroutine);
        void EndAllCoroutines();
    }
}