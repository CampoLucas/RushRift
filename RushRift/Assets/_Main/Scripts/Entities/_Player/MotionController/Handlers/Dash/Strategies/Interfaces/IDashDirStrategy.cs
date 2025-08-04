using System;
using UnityEngine;

namespace Game.Entities.Components.MotionController.Strategies
{
    public interface IDashDirStrategy : IDisposable
    {
        Vector3 GetDir(in MotionContext context, in DashConfig config);
    }
}