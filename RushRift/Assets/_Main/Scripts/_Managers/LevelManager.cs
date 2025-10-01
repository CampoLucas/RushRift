using System;
using Game.DesignPatterns.Observers;
using Game.VFX;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using System.Collections.Generic;
using Game.General;
using Game.UI.Screens;

namespace Game
{
    /// <summary>
    /// Manages the level state, including win and lose conditions.
    /// </summary>
    [AddComponentMenu("Game/Level Manager")]
    public class LevelManager : MonoBehaviour
    {
        #region Static Subjects

        public static readonly ISubject OnEnemyDeathSubject = new Subject();
        public static readonly ISubject OnEnemySpawnSubject = new Subject();
        public static readonly ISubject OnProjectileDestroyed = new Subject();

        #endregion

        #region Static Properties

        public static bool CanUseTerminal
        {
            get => _instance && _instance._canUseTerminals;
            set { if (_instance) _instance._canUseTerminals = value; }
        }

        public static bool HasDashDamage
        {
            get => _instance && _instance._hasDashDamage;
            set { if (_instance) _instance._hasDashDamage = value; }
        }
        
        public static bool BarrelInvulnerabilityEnabled
        {
            get => _instance && _instance._barrelInvulnerabilityEnabled;
            set { if (_instance) _instance._barrelInvulnerabilityEnabled = value; }
        }

        public static bool CanUseLockOnBlink
        {
            get => _instance && _instance._canUseLockOnBlink;
            set { if (_instance) _instance._canUseLockOnBlink = value; }
        }

        #endregion

        [Header("References")]
        [SerializeField] private LevelConfigSO levelConfig;
        [SerializeField] private EffectPool effectPool;
        
        [Header("Runtime Flags")]
        [SerializeField] private bool _barrelInvulnerabilityEnabled;

        private static LevelManager _instance;
        private TimerHandler _levelTimer = new();

        private ISubject _onGameOver = new Subject();
        private ISubject _onLevelWon = new Subject();
        private IObserver _onPlayerDeath;
        private IObserver _levelEnded;

        private Dictionary<UpgradeEnum, Entities.Effect> effectsReferencesDic = new();

        private bool _gameOver;
        private bool _gameOverNotified;
        private float _levelCompleteTime;
        private bool _canUseTerminals;
        private bool _hasDashDamage;
        private bool _canUseLockOnBlink;
        


        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            _onPlayerDeath = new ActionObserver(OnPlayerDeath);
            _levelEnded = new ActionObserver(OnLevelEndedHandler);

            _onLevelWon.Attach(_levelEnded);
            _onGameOver.Attach(_levelEnded);
        }

        private void Update()
        {
            if (!_gameOver) _levelTimer.DoUpdate(Time.deltaTime);
        }

        #region Static Methods

        #region Level Config
        public static LevelConfigSO GetLevelConfig() => _instance ? _instance.levelConfig : null;

        public static int GetLevelID()
        {
            if (_instance == null)
            {
#if UNITY_EDITOR
                Debug.LogError("ERROR: Trying to get the Level ID when the instance is null. Returning -1.");
#endif
                return -1;
            }

            var config = GetLevelConfig();
            if (!config)
            {
#if UNITY_EDITOR
                Debug.LogWarning("WARNING: Trying to get the Level ID from a Level without a config. Returning Scene index.");
#endif
                return SceneManager.GetActiveScene().buildIndex;
            }

            return config.LevelID;
        }

        public static bool TryGetLevelConfig(out LevelConfigSO config)
        {
            config = null;
            if (_instance == null)
            {
#if UNITY_EDITOR
                Debug.LogError("ERROR: Couldn't get the level config. The level manager is not instantiated.");
#endif

                return false;
            }

            config = GetLevelConfig();
            if (config)
            {
                return true;
            }
            
#if UNITY_EDITOR
            Debug.LogError("ERROR: Couldn't get the level config. The level config is null.");
#endif

            config = null;
            return false;
        }

        #endregion

        #region Subject Getters

        public static void GetPlayerReference(ISubject onDeath)
        {
            if (_instance == null) return;
            onDeath.Attach(_instance._onPlayerDeath);
        }

        public static bool TryGetGameOver(out ISubject subject)
        {
            if (_instance == null || _instance._onGameOver == null) { subject = null; return false; }
            subject = _instance._onGameOver; return true;
        }

        public static bool TryGetLevelWon(out ISubject subject)
        {
            if (_instance == null || _instance._onLevelWon == null) { subject = null; return false; }
            subject = _instance._onLevelWon; return true;
        }

        public static bool TryGetTimerSubject(out ISubject<float> subject)
        {
            subject = null;
            if (!_instance) return false;

            subject = _instance._levelTimer.OnTimeUpdated;
            return subject != null;
        }

        #endregion

        public static float LevelCompleteTime()
        {
            if (_instance) return _instance._levelTimer.CurrentTime;
            return 0;
        }

        public static bool TryGetVFX(VFXPrefabID id, VFXEmitterParams vfxParams, out EffectEmitter emitter)
        {
            if (!_instance) { emitter = null; return false; }

            if (_instance.effectPool.TryGetVFX(id, vfxParams, out emitter))
            {
#if UNITY_EDITOR
                Debug.Log($"LOG: TryGetVFX: Success || VFX: {emitter.gameObject.name}");
#endif
                return true;
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning("WARNING: TryGetVFX: Failure");
#endif
                return false;
            }
        }

        public static MedalInfo GetMedalInfo(MedalType type)
        {
            var data = SaveAndLoad.Load();
            var currLevel = GetLevelID();
            var config = GetLevelConfig();
            var medal = config.GetMedal(type);
            
            var endTime = LevelCompleteTime();

#if UNITY_EDITOR
            Debug.Log($"LOG: Getting {type} medal [Level: {currLevel} | End Time: {endTime} | Medal Time: {medal.requiredTime}]");
#endif

            var isUnlocked = data.IsMedalUnlocked(currLevel, type);
            return new MedalInfo(type.ToString(), medal.upgrade.EffectName, isUnlocked || endTime <= medal.requiredTime, isUnlocked, medal.requiredTime);
        }

        #endregion

        private void OnLevelEndedHandler()
        {
            _onLevelWon.Detach(_levelEnded);
            _onGameOver.Detach(_levelEnded);
            
            _gameOver = true;
        }
        
        private void OnPlayerDeath()
        {
            if (_gameOverNotified) return;
            _gameOverNotified = true;
            _gameOver = true;
            _onGameOver.NotifyAll();
        }

        private void OnDestroy()
        {
            _levelTimer.Dispose();
            _levelTimer = null;
            
            _onLevelWon.Dispose();
            _onGameOver.Dispose();
            _onPlayerDeath.Dispose();
            effectPool.Dispose();

            OnEnemyDeathSubject.DetachAll();
            OnEnemySpawnSubject.DetachAll();
            OnProjectileDestroyed.DetachAll();
            
            if (_instance == this)
                _instance = null; // limpiar la referencia estática
        }
    }
}
