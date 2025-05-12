using Game.Entities.Components;
using UnityEngine;

namespace Game.Entities.Components
{
    public interface IMovement : IEntityComponent
    {
        Vector3 Velocity { get; }
        bool Grounded { get; }
        float MaxSpeed { get; }
        float BaseMaxSpeed { get; }
        void AddMoveDir(Vector3 dir, bool normalize = true);
        void Move(Vector3 dir, float delta);
        void AddImpulse(Vector3 dir);
        void SetData(MovementData data);
        void AppendMaxSpeed(float amount);
        float MoveAmount();
        void EnableGravity(bool value);
    }
}