namespace Game.UI.Screens
{
    public class LevelWonModel : UIModel
    {
        public float EndTime { get; private set; }
        public float BestTime { get; private set; }
        public bool NewRecord { get; private set; }
        public bool LevelWon { get; private set; }
        public MedalInfo BronzeInfo { get; private set; }
        public MedalInfo SilverInfo { get; private set; }
        public MedalInfo GoldInfo { get; private set; }

        public void Initialize(float endTime, float bestTime, bool newRecord, MedalInfo bronze, MedalInfo silver, MedalInfo gold)
        {
            EndTime = endTime;
            BestTime = bestTime;
            NewRecord = newRecord;
            BronzeInfo = bronze;
            SilverInfo = silver;
            GoldInfo = gold;
            LevelWon = bronze.Unlocked || silver.Unlocked || gold.Unlocked;
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