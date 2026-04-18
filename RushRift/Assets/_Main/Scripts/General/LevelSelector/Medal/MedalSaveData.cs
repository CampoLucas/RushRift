using System;

namespace Game.General
{
    [Serializable]
    public struct MedalSaveData
    {
        // public bool bronzeUnlocked;
        // public bool silverUnlocked;
        // public bool goldUnlocked;
        
        /// <summary>
        /// -1 = auto (use highest unlocked), 0 = none, 1 = Bronze, 2 = Silver, 3 = Gold
        /// </summary>
        public int medalSelected;
        public int unlockedMedals;
        
        public bool Equals(MedalSaveData other)
        {
            return unlockedMedals == other.unlockedMedals && medalSelected == other.medalSelected;
        }
        
        public override bool Equals(object obj) => obj is MedalSaveData other && Equals(other);
        public static bool operator ==(MedalSaveData left, MedalSaveData right) => left.Equals(right);
        public static bool operator !=(MedalSaveData left, MedalSaveData right) => !left.Equals(right);
        
    }
}