using System.Runtime.CompilerServices;
using UnityEngine;

namespace Game.Utils
{
    public static class GameUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullOrMissingReference(this object obj)
        {
            if (ReferenceEquals(obj, null)) return true;
            if (obj is UnityEngine.Object o) return o == null;
            return false;
        }

        public static Vector3 GetOffsetPos(this Transform tr, Vector3 offset)
        {
            var x = tr.right * offset.x;
            var y = tr.up * offset.y;
            var z = tr.forward * offset.z;

            return tr.position + x + y + z;
        }
    }
}