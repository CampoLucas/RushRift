using System;
using System.Collections.Generic;
using Game.Saves;
using Game.Utils;
using MyTools.Global;
using UnityEditor;
using UnityEngine;

namespace Game.Levels
{
    [CreateAssetMenu(menuName = "Game/Levels/LevelRush", fileName = "New Level Rush Config")]
    public class LevelRushSO : BaseLevelSO
    {
        public Medal Bronze => bronze;
        public Medal Silver => silver;
        public Medal Gold => gold;

        [Header("Medals")]
        [SerializeField] private Medal bronze;
        [SerializeField] private Medal silver;
        [SerializeField] private Medal gold;

        [Header("Levels")]
        [SerializeField] private List<LevelConfigSO> levels;

        public override void LoadLevel()
        {
            //SceneHandler.LoadScene(scene.name);
        }

        public override bool IsUnlocked(List<BaseLevelSO> levelsList, int currIndex)
        {
            var goldUnlocked = -1;
            
            var data = SaveSystem.LoadGame();
            if (data == null)
            {
                this.Log("Couldn't find the save data.");
                return false;
            }
            
            for (var i = 0; i < levels.Count; i++)
            {
                var sector = levels[i];
                if (!sector)
                {
                    this.Log($"The level in the index {i} in the LevelRushSO is null. Returning false", LogType.Error);
                    return false;
                }
                
                if (data.IsMedalUnlocked(sector.LevelID, MedalType.Gold))
                {
                    goldUnlocked++;
                }
            }

            return goldUnlocked == levels.Count;
        }

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
}