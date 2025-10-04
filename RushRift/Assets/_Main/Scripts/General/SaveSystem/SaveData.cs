using System;
using Game.Entities;
using System.Collections.Generic;
using Game;
using Game.General;
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

        public bool IsMedalUnlocked(int currLevel, MedalType type)
        {
            if (!MedalsSaveData.TryGetValue(currLevel, out var saveData))
            {
                MedalsSaveData[currLevel] = saveData;
            }

            return type switch
            {
                MedalType.Bronze => saveData.bronzeUnlocked,
                MedalType.Silver => saveData.silverUnlocked,
                MedalType.Gold => saveData.goldUnlocked,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        public int TryGetUnlockedEffects(int levelID, out Effect[] unlockedEffects)
        {
            var unlocked = new List<Effect>();
            if (!LevelManager.TryGetLevelConfig(out var config))
            {
#if UNITY_EDITOR
                Debug.LogError("ERROR: Couldn't find the level config. returning.");
#endif

                unlockedEffects = unlocked.ToArray();
                return 0;
            }

            if (IsMedalUnlocked(levelID, MedalType.Bronze))
            {
                unlocked.Add(config.Bronze.upgrade);
            }

            if (IsMedalUnlocked(levelID, MedalType.Silver))
            {
                unlocked.Add(config.Silver.upgrade);
            }

            if (IsMedalUnlocked(levelID, MedalType.Gold))
            {
                unlocked.Add(config.Gold.upgrade);
            }

            unlockedEffects = unlocked.ToArray();
            return unlocked.Count;
        }

        public void UnlockMedal(int levelID, MedalType type)
        {
            if (!MedalsSaveData.TryGetValue(levelID, out var medalSaveData))
            {
                MedalsSaveData[levelID] = medalSaveData;
            }

            switch (type)
            {
                case MedalType.Bronze:
                    medalSaveData.bronzeUnlocked = true;
                    break;
                case MedalType.Silver:
                    medalSaveData.silverUnlocked = true;
                    break;
                case MedalType.Gold:
                    medalSaveData.goldUnlocked = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            MedalsSaveData[levelID] = medalSaveData;
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

        public SettingsData()
        {
            Version = Application.version;
        }

        private CameraSettings _camera = new();
        private SoundSettings _sound = new();
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
}



