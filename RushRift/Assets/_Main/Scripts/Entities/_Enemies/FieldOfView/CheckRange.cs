using UnityEngine;

namespace Game.Entities.Enemies.Components
{
    public class CheckRange : IPredicate<FOVParams>
    {
        private readonly float _range;

        public CheckRange(float range)
        {
            _range = range;
        }
        
        public bool Evaluate(ref FOVParams args)
        {
            return args.Distance < _range;
        }

        public void Dispose()
        {
            
        }
    }
}