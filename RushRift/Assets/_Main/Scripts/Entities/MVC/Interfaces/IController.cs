using System;
using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

namespace Game.Entities
{
    public interface IController : IDisposable
    {
        Transform Origin { get; }
        Joints<EntityJoint> Joints { get; }
        IModel GetModel();
        IView GetView();
        VisualEffect SpeedLines { get; }

        Vector3 MoveDirection();

        Coroutine DoCoroutine(IEnumerator routine);
        void EndCoroutine(Coroutine coroutine);
        void EndAllCoroutines();
    }
}