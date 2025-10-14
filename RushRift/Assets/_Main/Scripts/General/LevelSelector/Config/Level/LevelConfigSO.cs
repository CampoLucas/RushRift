using System;
using System.Collections.Generic;
using Game.Saves;
using Game.Utils;
using MyTools.Global;
using Tools.Scripts.PropertyAttributes;
using UnityEngine;

namespace Game.Levels
{
    [CreateAssetMenu(menuName = "Game/Levels/LevelSector", fileName = "New Level Sector Config")]
    public class LevelConfigSO : BaseLevelSO
    {
        public Medal Bronze => bronze;
        public Medal Silver => silver;
        public Medal Gold => gold;

        [Header("Medals")]
        [SerializeField] private Medal bronze;
        [SerializeField] private Medal silver;
        [SerializeField] private Medal gold;

        [Header("Scene")]
        [ReadOnly, SerializeField] private string sceneName;
#if UNITY_EDITOR
        [SerializeField] private UnityEditor.SceneAsset scene;
#endif

        public override void LoadLevel()
        {
            SceneHandler.LoadScene(sceneName);
        }

        public override bool IsUnlocked(List<BaseLevelSO> levelsList, int currIndex)
        {
            if (currIndex == 0)
            {
                return true;
            }

            var prevLevel = levelsList[currIndex - 1];
            if (!prevLevel)
            {
                this.Log("The previous level shouldn't be null.", LogType.Warning);
                return false;
            }

            var data = SaveSystem.LoadGame();
            if (data == null)
            {
                this.Log("Couldn't find the save data.");
                return false;
            }

            return data.IsMedalUnlocked(prevLevel.LevelID, MedalType.Bronze);
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
        
        private void OnValidate()
        {
#if UNITY_EDITOR
            if (scene) sceneName = scene.name;
            else sceneName = SceneHandler.FirstLevel;
#endif
        }
    }
    
    
}

