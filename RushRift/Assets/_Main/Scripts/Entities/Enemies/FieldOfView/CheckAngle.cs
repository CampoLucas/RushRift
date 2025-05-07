using UnityEngine;

namespace Game.Entities.Enemies.Components
{
    public class CheckAngle : IPredicate<FOVParams>
    {
        private readonly float _angle;

        public CheckAngle(float angle)
        {
            _angle = angle;
        }

        public bool Evaluate(ref FOVParams args)
        {
            return _angle * .5f > args.Angle;
        }

        public void Dispose()
        {
            
        }
    }
}