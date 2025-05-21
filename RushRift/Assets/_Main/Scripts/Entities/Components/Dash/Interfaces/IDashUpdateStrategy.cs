using System;
using UnityEngine;

namespace Game.Entities.Components
{
    public interface IDashUpdateStrategy : IDisposable
    {
        bool OnDashUpdate(Transform transform, Vector3 currentPosition);
        void Reset();
    }
}