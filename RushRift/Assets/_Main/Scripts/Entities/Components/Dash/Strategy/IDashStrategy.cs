using System;
using UnityEngine;

namespace Game.Entities.Components
{
    public interface IDashStrategy : IDisposable
    {
        void StartDash(Transform transform, Transform cameraTransform, out Vector3 start, out Vector3 end, out Vector3 dashDir);
    }
}