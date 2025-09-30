using System;
using Game.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.General
{
    [CreateAssetMenu(menuName = "Game/LevelConfig", fileName = "New LevelConfig")]
    public class LevelConfigSO : ScriptableObject
    {
        public int LevelID => levelID;
        public string LevelName => LevelName;
        public Medal Bronze => bronze;
        public Medal Silver => silver;
        public Medal Gold => gold;

        [Header("Settings")]
        [SerializeField] private int levelID;
        [SerializeField] private string levelName;

        [Header("Medals")]
        [SerializeField] private Medal bronze;
        [SerializeField] private Medal silver;
        [SerializeField] private Medal gold;

        public Medal GetMedal(MedalType type)
        {
            return type switch
            {
                MedalType.Bronze => Bronze,
                MedalType.Silver => Silver,
                MedalType.Gold   => Gold,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
    }

    [Serializable]
    public struct Medal
    {
        public float requiredTime;
        public Effect upgrade;
    }

    [Serializable]
    public struct MedalSaveData
    {
        public bool bronzeUnlocked;
        public bool silverUnlocked;
        public bool goldUnlocked;
    }
    
    public enum MedalType
    {
        Bronze,
        Silver,
        Gold
    }
}

