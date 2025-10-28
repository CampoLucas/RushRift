using System;

namespace Game.General
{
    [Serializable]
    public struct MedalSaveData
    {
        public bool bronzeUnlocked;
        public bool silverUnlocked;
        public bool goldUnlocked;
        
        public bool Equals(MedalSaveData other)
        {
            return bronzeUnlocked == other.bronzeUnlocked &&
                   silverUnlocked == other.silverUnlocked &&
                   goldUnlocked == other.goldUnlocked;
        }
        
        public override int GetHashCode()
        {
            // Compact hash code from bools
            return (bronzeUnlocked ? 1 : 0) |
                   ((silverUnlocked ? 1 : 0) << 1) |
                   ((goldUnlocked ? 1 : 0) << 2);
        }
        
        public static bool operator ==(MedalSaveData left, MedalSaveData right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MedalSaveData left, MedalSaveData right)
        {
            return !left.Equals(right);
        }
    }
}