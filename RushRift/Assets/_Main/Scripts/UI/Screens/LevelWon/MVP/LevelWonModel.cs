using System;
using System.Collections.Generic;
using System.Linq;
using Game.General;
using Game.Levels;
using Unity.VisualScripting;

namespace Game.UI.Screens
{
    public sealed class LevelWonModel : UIModel
    {
        public float EndTime { get; private set; }
        public float BestTime { get; private set; }
        public bool NewRecord { get; private set; }
        public bool LevelWon { get; private set; }

        public List<MedalType> MedalInfos { get; private set; } = new();
        private Dictionary<MedalType, MedalInfo> _medalDict = new();

        public void Initialize(float endTime, float bestTime, bool newRecord, Dictionary<MedalType, MedalInfo> medals)
        {
            EndTime = endTime;
            BestTime = bestTime;
            NewRecord = newRecord;

            MedalInfos.Clear();
            _medalDict.Clear();
            
            
            if (medals != null)
            {
                MedalInfos.AddRange(medals.Keys);
                _medalDict.AddRange(medals);
                
            }
            
            LevelWon = HasWon();
        }

        private bool HasWon()
        {
            var count = MedalInfos.Count;
            if (count == 0) return true;
            
            for (var i = 0; i < count; i++)
            {
                if (_medalDict[MedalInfos[i]].Unlocked)
                {
                    return true;
                }
            }

            return false;
        }

        public override void Reset()
        {
            base.Reset();
            Initialize(0, 0, false, null);
        }

        public bool IsMedalUnlocked(MedalType type)
        {
            return _medalDict.TryGetValue(type, out var medal) && medal.Unlocked;
        }

        public bool HasMedal(MedalType type)
        {
            return _medalDict.ContainsKey(type);
        }

        public bool TryGetMedal(MedalType type, out MedalInfo info)
        {
            return _medalDict.TryGetValue(type, out info);
        }
    }

    public struct MedalInfo
    {
        public string Name { get; private set; }
        public string UpgradeName { get; private set; }
        public bool Unlocked { get; private set; }
        public bool PrevUnlocked { get; private set; }
        public float MedalTime { get; private set; }

        public MedalInfo(string medal, string upgrade, bool unlocked, bool prevUnlocked, float medalTime)
        {
            Name = medal;
            UpgradeName = upgrade;
            Unlocked = unlocked;
            PrevUnlocked = prevUnlocked;
            MedalTime = medalTime;
        }
    }
}