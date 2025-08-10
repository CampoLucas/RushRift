using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Detection
{
    [System.Serializable]
    public class LineDetectData
    {
        public int MaxOverlaps => maxOverlaps;
        
        [Header("Offset")]
        [SerializeField] private Vector3 offset;
        
        [Header("Direction")] 
        [SerializeField] private DirectionEnum direction;
        [SerializeField] private Vector3 custom;
        [SerializeField] private bool originRelative;

        [Header("Detection")]
        [SerializeField] private LayerMask target;
        [SerializeField] private int maxOverlaps = 1;

        [Header("Length")]
        [SerializeField] private float length = 10;
        [SerializeField] private bool blockByObstacle;
        [SerializeField] private LayerMask obstacle;
        
        public LineDetect Get(Transform origin)
        {
            return new LineDetect(origin, this);
        }
        
        public Vector3 GetPosOffset(Transform origin)
        {
            var x = origin.right * offset.x;
            var y = origin.up * offset.y;
            var z = origin.forward * offset.z;

            return origin.transform.position + x + y + z;
        }

        public Vector3 GetDir(Transform origin)
        {
            switch (direction)
            {
                case DirectionEnum.Forward:
                    return originRelative ? origin.forward : Vector3.forward;
                case DirectionEnum.Back:
                    return originRelative ? -origin.forward : Vector3.back;
                case DirectionEnum.Right:
                    return originRelative ? origin.right : Vector3.right;
                case DirectionEnum.Left:
                    return originRelative ? -origin.right : Vector3.left;
                case DirectionEnum.Up:
                    return originRelative ? origin.up : Vector3.up;
                case DirectionEnum.Down:
                    return originRelative ? -origin.up : Vector3.down;
                case DirectionEnum.Custom:
                    return custom;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public int Detect(Transform origin, ref RaycastHit[] hits, out Vector3 endPos, out bool blocked, out RaycastHit blockHit)
        {
            var pos = GetPosOffset(origin);
            var dir = GetDir(origin);
            
            //Debug.DrawRay(pos, dir * length, Color.magenta);
            
            var ray = new Ray(pos, dir);
            var laserLength = length;
            blockHit = default;
            blocked = false;
            
            if (blockByObstacle && Physics.Raycast(pos, dir, out blockHit, length, obstacle))
            {
                laserLength = Vector3.Distance(pos, blockHit.point);
                blocked = true;
            }

            endPos = pos + dir * laserLength;
            var overlaps = Physics.RaycastNonAlloc(pos, dir, hits, laserLength, target);
            //var overlaps = Physics.Raycast(pos, dir, out var hit, laserLength, target) ? 1 : 0;
            Debug.Log($"Overlaps {overlaps}, lenght: {length}, blocked: {blocked}");
            return overlaps;
        }

        public void Draw(Transform origin, Color color)
        {
            var pos = GetPosOffset(origin);
            var dir = GetDir(origin);

            Gizmos.color = color;
            Gizmos.DrawLine(pos, pos + (dir * length));
            //Gizmos.DrawRay(pos, dir * length);
        }
    }

    public enum DirectionEnum
    {
        Forward,
        Back,
        Right,
        Left,
        Up,
        Down,
        Custom,
    }
}