using System.Runtime.CompilerServices;
using UnityEngine;

namespace GrayboxTools
{
    public static class Utils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullOrMissing(this object obj)
        {
            if (ReferenceEquals(obj, null)) return true;
            if (obj is UnityEngine.Object o) return o == null;
            return false;
        }
    }
}