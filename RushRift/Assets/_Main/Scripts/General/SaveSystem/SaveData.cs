using System;
using Game.Entities;
using System.Collections.Generic;
using Game;
using Game.General;
using Game.Levels;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;


namespace Game.Saves
{
    [System.Serializable]

    public class SaveData : BaseSaveData
    {
        public string LastScene => _lastScene;
        public int lastSceneIndex => _lastSceneIndex;


        private string _lastScene;
        private int _lastSceneIndex;

        /// <summary>
        /// A property that in the case someone plays with an old save that didn't had the BestTime dictionary, it creates it.
        /// </summary>
        public Dictionary<int, float> BestTimes
        {
            get => _bestTimes ??= new Dictionary<int, float>();
            private set => _bestTimes = value;
        }

        private Dictionary<int, MedalSaveData> MedalsSaveData
        {
            get => _levelsMedalsTimes ??= new Dictionary<int, MedalSaveData>();
            set => _levelsMedalsTimes = value;
        }

        private Dictionary<int, float> _bestTimes = new();
        private Dictionary<int, MedalSaveData> _levelsMedalsTimes = new();

        public SaveData()
        {
            Version = Application.version;
        }

        public void CheckBestTime(int level, float currTime, out float prevBest, out float currBest, out bool newRecord)
        {
            if (!BestTimes.TryGetValue(level, out var bestTime) || bestTime < 0f)
                bestTime = -1f;

            prevBest = bestTime;
            newRecord = bestTime < 0f || currTime < bestTime;
            currBest = newRecord ? currTime : bestTime;
        }

        public void SetNewBestTime(int level, float newBest) => BestTimes[level] = newBest;

        #region Medal Methods

        /// <summary>
        /// Returns the medal selected
        /// </summary>
        /// <param name="levelId"></param>
        /// <returns>
        /// -1 = Default, 0 = None, 1 = Bronze, 2 = Silver, 3 = Gold
        /// </returns>
        public int GetLevelMedalSelection(int levelId)
        {
            if (!MedalsSaveData.TryGetValue(levelId, out var data) || data.medalSelected < 0)
            {
                if (IsMedalUnlocked(levelId, MedalType.Gold))
                {
                    return 3;
                }
                if (IsMedalUnlocked(levelId, MedalType.Silver))
                {
                    return 2;
                }
                if (IsMedalUnlocked(levelId, MedalType.Bronze))
                {
                    return 1;
                }
                
                return 0;
            }

            return data.medalSelected;
        }
        
        public int GetUnlockedMedals(int levelId)
        {
            if (!MedalsSaveData.TryGetValue(levelId, out var data))
            {
                return 0;
            }

            return data.unlockedMedals;
        }

        public void SetLevelMedalSelection(int levelId, int selection)
        {
            if (!MedalsSaveData.TryGetValue(levelId, out var data))
            {
                data = new MedalSaveData
                {
                    medalSelected = -1
                };
            }

            data.medalSelected = selection;
            MedalsSaveData[levelId] = data;
        }
        
        public MedalSaveData GetMedalSaveData(int levelId)
        {
            if (!MedalsSaveData.TryGetValue(levelId, out var saveData))
            {
                MedalsSaveData[levelId] = saveData;
            }

            return saveData;
        }

        /// <summary>
        /// If unlocks a new medal, clears the selection. Sets the selected int to -1.
        /// </summary>
        /// <param name="levelId"></param>
        /// <param name="newType"></param>
        public void OnMedalUnlocked(int levelId, int prevMedals, int newMedals)
        {
            var sel = GetUnlockedMedals(levelId);
            if (sel <= 0) return; // Already on default selection. ToDO: make a setting for this.

            //var selMedal = SaveData.SelectionToMedalType(sel);
            if (newMedals > prevMedals)
            {
                SetLevelMedalSelection(levelId, -1);
            }
            
        }
        
        public static MedalType? SelectionToMedalType(int selection) => selection switch
        {
            1 => MedalType.Bronze,
            2 => MedalType.Silver,
            3 => MedalType.Gold,
            _ => null
        };
        
        public bool IsMedalUnlocked(int currLevel, MedalType type)
        {
            if (!MedalsSaveData.TryGetValue(currLevel, out var saveData))
            {
                MedalsSaveData[currLevel] = saveData;
            }

            return type switch
            {
                MedalType.Bronze => saveData.unlockedMedals > 0,
                MedalType.Silver => saveData.unlockedMedals > 1,
                MedalType.Gold => saveData.unlockedMedals > 2,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
        
        public bool CanUseMedal(int currLevel, MedalType type)
        {
            if (!MedalsSaveData.TryGetValue(currLevel, out var saveData))
            {
                MedalsSaveData[currLevel] = saveData;
            }

            var medals = GetLevelMedalSelection(currLevel);
            

            return type switch
            {
                MedalType.Bronze => medals > 0,
                MedalType.Silver => medals > 1,
                MedalType.Gold => medals > 2,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        public int TryGetUnlockedEffects(int levelID, out Effect[] unlockedEffects)
        {
            var unlocked = new List<Effect>();
            if (!GlobalLevelManager.CurrentLevel.TryGet(out var config))
            {
#if UNITY_EDITOR
                Debug.LogError("ERROR: Couldn't find the level config. returning.");
#endif

                unlockedEffects = unlocked.ToArray();
                return 0;
            }

            if (TryGetUpgrade(levelID, MedalType.Bronze, config, out var upgrade))
            {
                unlocked.Add(upgrade);
            }
            
            if (TryGetUpgrade(levelID, MedalType.Silver, config, out upgrade))
            {
                unlocked.Add(upgrade);
            }
            
            if (TryGetUpgrade(levelID, MedalType.Gold, config, out upgrade))
            {
                unlocked.Add(upgrade);
            }

            unlockedEffects = unlocked.ToArray();
            return unlocked.Count;
        }

        private bool TryGetUpgrade(int levelID, MedalType type, BaseLevelSO config, out Effect upgrade)
        {
            if (CanUseMedal(levelID, type) && config.TryGetMedal(type, out var bronze))
            {
                upgrade = bronze.upgrade;
                return true;
            }

            upgrade = default;
            return false;
        }

        public void UnlockMedal(int levelId, MedalType type)
        {
            if (!MedalsSaveData.TryGetValue(levelId, out var medalSaveData))
            {
                MedalsSaveData[levelId] = medalSaveData;
            }

            var prevMedals = medalSaveData.unlockedMedals;
            var unlockedMedals = medalSaveData.unlockedMedals;
            
            switch (type)
            {
                case MedalType.Bronze:
                    if (prevMedals < 1) unlockedMedals = 1;
                    break;
                case MedalType.Silver:
                    if (prevMedals < 2) unlockedMedals = 2;
                    break;
                case MedalType.Gold:
                    if (prevMedals < 3) unlockedMedals = 3;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            medalSaveData.unlockedMedals = unlockedMedals;
            MedalsSaveData[levelId] = medalSaveData;
            OnMedalUnlocked(levelId, prevMedals, unlockedMedals);
        }
        
        public int GetUnlockedMedalsCount(int currLevel)
        {
            if (!MedalsSaveData.TryGetValue(currLevel, out var saveData))
            {
                MedalsSaveData[currLevel] = saveData;
            }

            var medalsUnlocked = saveData.unlockedMedals;
            return medalsUnlocked;
        }

        #endregion

        #region Last scene

        public void SetLastScene(string scene, int index)
        {
#if UNITY_EDITOR
            Debug.Log($"[SaveData] Saving scene {scene}({index})");
#endif
            
            _lastScene = scene;
            _lastSceneIndex = index;
        }

        #endregion
    }

    [Serializable]
    public class SettingsData : BaseSaveData
    {
        /// <summary>
        /// A property that in the case someone plays with an old save that didn't had the Camera class, it creates it.
        /// </summary>
        public CameraSettings Camera
        {
            get => _camera ??= new CameraSettings();
            private set => _camera = value;
        }

        /// <summary>
        /// A property that in the case someone plays with an old save that didn't had the Sound class, it creates it.
        /// </summary>
        public SoundSettings Sound
        {
            get => _sound ??= new SoundSettings();
            private set => _sound = value;
        }

        public CustomSettings Custom
        {
            get => _custom ??= new CustomSettings();
            private set => _custom = value;
        }

        public SettingsData()
        {
            Version = Application.version;
        }
        

        private CameraSettings _camera = new();
        private SoundSettings _sound = new();
        private CustomSettings _custom = new();

        public static Color GetBlinkChargeColor()
        {
            //var settings = SaveSystem.LoadSettings();
            return Color.white;
        }
    }

    [Serializable]
    public class BaseSaveData
    {
        public string Version
        {
            get => _version.ToString();
            set
            {
                if (!System.Version.TryParse(value, out var parsed))
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"[SaveSystem] Invalid version string '{value}', defaulting to 0.0.0");
#endif
                    parsed = new Version(0, 0, 0);
                }
                _version = parsed;
            }
        }
        
        private Version _version; // stored game version
    }


    [Serializable]
    public class CameraSettings
    {
        public float sensibility = .1f;
        public float smoothness = 30;
        public bool invertX = false;
        public bool invertY = false;
    }

    [Serializable]
    public class SoundSettings
    {
        public float masterVolume = 1;
        public float musicVolume = 1;
        public float sfxVolume = 1;
    }

    [Serializable]
    public class CustomSettings
    {
        //public Vector3 blinkChargeColor = Color.red;
    }
}



