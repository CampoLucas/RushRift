using UnityEngine;

namespace Game.Entities.Enemies.Components
{
    public class CheckView : IPredicate<FOVParams>
    {
        private readonly LayerMask _mask; // wall masks, etc.

        public CheckView(LayerMask mask)
        {
            _mask = mask;
        }

        public bool Evaluate(ref FOVParams args)
        {
            return !Physics.Raycast(args.OriginPosition, args.Direction, args.Distance, _mask);
        }

        public void Dispose()
        {
            
        }
    }
}