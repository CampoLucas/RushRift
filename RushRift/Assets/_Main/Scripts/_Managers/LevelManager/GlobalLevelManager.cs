using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.DesignPatterns.Observers;
using Game.Levels;
using Game.Levels.SingleLevel;
using Game.Saves;
using Game.UI.Screens;
using Game.Utils;
using MyTools.Global;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Game/Global Level Manager")]
    public sealed class GlobalLevelManager : SingletonBehaviour<GlobalLevelManager>
    {
        #region Public Properties

        public static NullCheck<BaseLevelSO> CurrentLevel { get; private set; }
        public static bool GameOver { get; private set; }
        public static float CompleteTime { get; private set; }
        public int LevelIndex { get; set; } = -1;
        public bool ReachedNextZone { get; set; }

        public static bool LoadingLevel
        {
            get => _loadingLevel;
            set
            {
                Debug.Log($"[SuperTest] LoadingLevel set to {value}");
                _loadingLevel = value;
            }
        }

        private static bool _loadingLevel;

        public static bool DashDamage => _instance && _instance.TryGet(out var instance) && instance.Flags.DashDamage;
        public static bool PowerSurge => _instance && _instance.TryGet(out var instance) && instance.Flags.PowerSurge;
        public static bool BarrelInvulnerability => _instance && _instance.TryGet(out var instance) && instance.Flags.BarrelInvulnerability;
        public static bool Blink => _instance && _instance.TryGet(out var instance) && instance.Flags.Blink;
        
        private LevelFlags Flags;

        #endregion

        private static NullCheck<BaseLevelSO> _currentLevel;
        private readonly List<string> _loadedLevels = new();
        private readonly Dictionary<string, Scene> _loadedLevelsDict = new();
        private TimerHandler _levelTimer = new();
        private DesignPatterns.Observers.IObserver<bool> _gameOverObserver;
        
        
        
        protected override void OnAwake()
        {
            base.OnAwake();
            // Reset the global events
            GlobalEvents.Reset();
            
            // Create observers
            _gameOverObserver = new ActionObserver<bool>(OnGameOverHandler);

            // Attach observers
            GlobalEvents.GameOver.Attach(_gameOverObserver);
            
            // Set as loading
            LoadingLevel = true;
        }

        private void Update()
        {
            if (!GameOver)
            {
                _levelTimer.DoUpdate(Time.deltaTime);
            }
        }

        public async UniTask LoadLevelAsync(BaseLevelSO level)
        {
            if (level == null)
            {
                this.Log("LevelSO is null");
                return;
            }

            // Unload previously loaded levels
            await UnloadAllLevelsAsync();
            
            // Load the new level additively
            await level.LoadAsync(this);
            CurrentLevel = level;
        }

        public async UniTask LoadLevelSceneAsync(string sceneName, bool preloaded = false)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                this.Log("Invalid scene name");
                return;
            }

            if (_loadedLevelsDict.ContainsKey(sceneName))
            {
                return;
            }
            
            var op = SceneHandler.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            
            // If preloaded, don't await it. Let it load in background
            if (!preloaded)
            {
                await op.ToUniTask();
            }

            var scene = SceneHandler.GetSceneByName(sceneName);
            if (!_loadedLevelsDict.ContainsKey(sceneName))
            {
                _loadedLevels.Add(sceneName);
                _loadedLevelsDict[sceneName] = scene;
            }
        }

        public async UniTask UnloadSceneAsync(string sceneName)
        {
            if (!_loadedLevelsDict.TryGetValue(sceneName, out var scene))
            {
                return;
            }

            await SceneHandler.UnloadSceneAsync(scene);
            _loadedLevelsDict.Remove(sceneName);
            _loadedLevels.Remove(sceneName);
        }
        
        public async UniTask UnloadAllLevelsAsync()
        {
            for (var i = 0; i < _loadedLevels.Count; i++)
            {
                var loadedScenes = _loadedLevelsDict[_loadedLevels[i]];
                await SceneHandler.UnloadSceneAsync(loadedScenes);
            }

            _loadedLevels.Clear();
            _loadedLevelsDict.Clear();
            CurrentLevel = null;
            LevelIndex = -1;
        }
        
        protected override bool CreateIfNull() => false;
        protected override bool DontDestroy() => false;

        protected override void OnDisposeNotInstance()
        {
            _levelTimer.Dispose();
            _levelTimer = null;

        }

        protected override void OnDisposeInstance()
        {
            GlobalEvents.GameOver.Detach(_gameOverObserver);
            _gameOverObserver.Dispose();
            
            GlobalEvents.Reset();
        }


        #region Flag Setters

        public static void SetDashDamage(bool b)
        {
            if (!_instance.TryGet(out var manager)) return;
            manager.Flags.DashDamage = b;
        }
        
        public static void SetPowerSurge(bool b)
        {
            if (!_instance.TryGet(out var manager)) return;
            manager.Flags.PowerSurge = b;
        }
        
        public static void SetBarrelInvulnerability(bool b)
        {
            if (!_instance.TryGet(out var manager)) return;
            manager.Flags.BarrelInvulnerability = b;
        }
        
        public static void SetBlink(bool b)
        {
            if (!_instance.TryGet(out var manager)) return;
            manager.Flags.Blink = b;
        }

        #endregion

        #region Observer Handlers

        private void OnGameOverHandler(bool playerWon)
        {
            GlobalEvents.GameOver.Detach(_gameOverObserver);
            CompleteTime = _levelTimer.CurrentTime;
        }
        
        private void OnPlayerDeath()
        {
            GlobalEvents.GameOver.NotifyAll(false);
        }

        #endregion

        #region Level Getters

        public static int GetID()
        {
            if (!CurrentLevel.TryGet(out var config))
            {
#if UNITY_EDITOR
                Debug.LogError($"ERROR: Trying to get the {typeof(GlobalLevelManager)} doesn't . Returning -1.");
#endif

                return -1;
            }

            return config.LevelID;
        }
        
        public static bool TryGetLevelConfig(out BaseLevelSO config)
        {
            return CurrentLevel.TryGet(out config);
        }

        #endregion

        #region Medal Methods

        public static MedalInfo GetMedalInfo(MedalType type)
        {
            var data = SaveSystem.LoadGame();
            var currLevel = GetID();
            if (!TryGetLevelConfig(out var config))
            {
                Debug.LogError($"ERROR: Getting {type} medal [Level: {currLevel}] config not found.");
                return default;
            }
            
            var medal = config.GetMedal(type);
            var endTime = CompleteTime;

#if UNITY_EDITOR
            Debug.Log($"LOG: Getting {type} medal [Level: {currLevel} | End Time: {endTime} | Medal Time: {medal.requiredTime}]");
#endif

            var isUnlocked = data.IsMedalUnlocked(currLevel, type);
            return new MedalInfo(type.ToString(), medal.upgrade.EffectName, isUnlocked || endTime <= medal.requiredTime, isUnlocked, medal.requiredTime);
        }

        #endregion
    }
}