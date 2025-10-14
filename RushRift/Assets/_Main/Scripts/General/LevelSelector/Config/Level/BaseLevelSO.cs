using System;
using System.Collections.Generic;
using Game.Utils;
using UnityEngine;

namespace Game.Levels
{
    public abstract class BaseLevelSO : SerializableSO
    {
        public int LevelID => levelID;
        public string LevelName => levelName;
        
        [Header("Settings")]
        [SerializeField] private int levelID;
        [SerializeField] private string levelName;


        public abstract void LoadLevel();
        public abstract bool IsUnlocked(List<BaseLevelSO> levelsList, int currIndex);
    }
}