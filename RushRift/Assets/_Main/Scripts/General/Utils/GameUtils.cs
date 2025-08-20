namespace Game.Utils
{
    public static class GameUtils
    {
        public static bool IsNullOrMissingReference(this object obj)
        {
            if (ReferenceEquals(obj, null)) return true;
            if (obj is UnityEngine.Object o) return o == null;
            return false;
        }
    }
}