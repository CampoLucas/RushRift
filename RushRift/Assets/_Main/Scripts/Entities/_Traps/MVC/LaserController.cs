using Game.Entities;
using UnityEngine;

namespace Game.Entities
{
    public class LaserController : EntityController
    {
        public sealed override Vector3 MoveDirection()
        {
            return Vector3.zero;
        }
    }
}