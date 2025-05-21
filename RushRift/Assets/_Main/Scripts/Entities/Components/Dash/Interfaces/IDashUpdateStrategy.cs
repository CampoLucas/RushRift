using System;
using UnityEngine;

namespace Game.Entities.Components
{
    public interface IDashUpdateStrategy : IDisposable
    {
        void OnDashUpdate(Transform transform, Vector3 currentPosition);
    }
}