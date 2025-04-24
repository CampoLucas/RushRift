using UnityEngine;

namespace Game.Utils
{
    public static class VectorUtils
    {
        public static Vector3 XOZ(this Vector2 target) => new Vector3(target.x, 0, target.y);

        public static Vector3 TransformOffset(this Vector3 target, Transform origin)
        {
            if (!origin) return Vector3.zero;
            return origin.position + (origin.right * target.x) + (origin.up * target.y) + (origin.forward * target.z);
        }
    }
}