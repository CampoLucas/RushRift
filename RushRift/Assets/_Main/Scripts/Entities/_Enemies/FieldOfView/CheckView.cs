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
            var pos = args.OriginPosition;
            var dir = args.Direction;
            var dis = args.Distance;
            
            var inView = !Physics.Raycast(pos, dir, dis, _mask);
#if UNITY_EDITOR
            Debug.DrawRay(pos, dir * dis, inView ? Color.green : Color.red);      
#endif
            return inView;
        }

        public void Dispose()
        {
            
        }
    }
}