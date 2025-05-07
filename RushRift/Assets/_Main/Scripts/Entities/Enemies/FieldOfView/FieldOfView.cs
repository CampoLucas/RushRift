using UnityEngine;

namespace Game.Entities.Enemies.Components
{
    public class FieldOfView : IPredicate<FOVParams>
    {
        private IPredicate<FOVParams>[] _predicates;
        private FOVParams _params;
        private bool _ifAny;

        public FieldOfView(IPredicate<FOVParams>[] predicates, bool ifAny = false)
        {
            _predicates = predicates;
            _ifAny = ifAny;
        }

        public bool Evaluate(ref FOVParams args)
        {
            if (_predicates == null || _predicates.Length == 0) return false;

            var result = !_ifAny;
            for (var i = 0; i < _predicates.Length; i++)
            {
                var p = _predicates[i];
                var evaluate = p.Evaluate(ref args);
                
                if ((_ifAny && !evaluate) || (!_ifAny && evaluate)) continue;
                result = _ifAny;
                break;
            }

            return result;
        }

        public void Dispose()
        {
            if (_predicates != null && _predicates.Length > 0)
            {
                for (var i = 0; i < _predicates.Length; i++)
                {
                    _predicates[i].Dispose();
                    _predicates[i] = null;
                }

                _predicates = null;
            }
        }
    }

    /// <summary>
    /// Stores all the values of the target and origin, that way it doesn't have to get them for each FOVPredicate
    /// </summary>
    public struct FOVParams
    {
        public Vector3 OriginPosition;
        public Vector3 OriginForward;
        public Vector3 TargetPosition;
        public Vector3 Direction;
        public float Distance;
        public float Angle;

        public static FOVParams GetFOVParams(Vector3 originPosition, Vector3 originForward, Vector3 targetPos)
        {
            var diff = targetPos - originPosition;
            var dir = diff.normalized;

            return new FOVParams()
            {
                OriginPosition = originPosition,
                OriginForward = originForward,
                TargetPosition = targetPos,
                Direction = dir,
                Distance = diff.magnitude,
                Angle = Vector3.Angle(originForward, dir),
            };
        }
    }
}