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
        public static readonly ISubject OnEnemyDeathSubject = new Subject();
        public static readonly ISubject OnEnemySpawnSubject = new Subject();
        public static readonly ISubject OnProjectileDestroyed = new Subject();
        public static readonly ISubject<float> OnTimeUpdated = new Subject<float>(); 

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

        [Header("References")]
        [SerializeField] private LevelConfigSO levelConfig;
        [SerializeField] private EffectPool effectPool;
        
        [Header("Runtime Flags")]
        [SerializeField] private bool _barrelInvulnerabilityEnabled;

        private static LevelManager _instance;

        private ISubject _onGameOver = new Subject();
        private ISubject _onLevelWon = new Subject();
        private IObserver _onPlayerDeath;
        private IObserver _onEnemyDeath;

        private Dictionary<UpgradeEnum, Entities.Effect> effectsReferencesDic = new();

        private int _allEnemies;
        private int _deadEnemies;
        private bool _gameOver;
        private bool _gameOverNotified;
        private float _levelCompleteTime;
        private bool _canUseTerminals;
        private bool _hasDashDamage;
        private bool _canUseLockOnBlink;
        
        public static bool HasAppliedOwnedUpgradesThisScene { get; private set; }


        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
            }
            
            _instance = this;
            _onPlayerDeath = new ActionObserver(OnPlayerDeath);
            _onEnemyDeath = new ActionObserver(OnEnemyDeath);
        }

        public static void GetPlayerReference(ISubject onDeath)
        {
            if (_instance == null) return;
            onDeath.Attach(_instance._onPlayerDeath);
        }

        public static Entities.Effect GetEffect(UpgradeEnum upgrade)
        {
            return _instance.effectsReferencesDic[upgrade];
        }

        public static LevelConfigSO GetLevelConfig() => _instance ? _instance.levelConfig : null;

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

        public static void GetEnemiesReference(ISubject onDeath)
        {
            if (_instance == null) return;
            onDeath.Attach(_instance._onEnemyDeath);
            _instance._allEnemies += 1;
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

        public static bool IsGameOver()
        {
            if (_instance == null) return true;
            return _instance._gameOver;
        }

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

        public static float LevelCompleteTime()
        {
            if (_instance) return _instance._levelCompleteTime;
            return 0;
        }

        public static void SetLevelCompleteTime(float time)
        {
            if (_instance) _instance._levelCompleteTime = time;
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
            var config = LevelManager.GetLevelConfig();
            var medal = config.GetMedal(type);
            
            var endTime = LevelCompleteTime();

#if UNITY_EDITOR
            Debug.Log($"LOG: Getting {type} medal [Level: {currLevel} | End Time: {endTime} | Medal Time: {medal.requiredTime}]");
#endif
            
            return new MedalInfo(type.ToString(), medal.upgrade.EffectName, endTime <= medal.requiredTime, data.IsMedalUnlocked(currLevel, type), medal.requiredTime);
        }
        
        private void OnPlayerDeath()
        {
            if (_gameOverNotified) return;
            _gameOverNotified = true;
            _gameOver = true;
            _onGameOver.NotifyAll();
        }

        private void OnEnemyDeath()
        {
            _deadEnemies += 1;
        }
        
        public static bool TryGetActiveMedalTimes(out float bronze, out float silver, out float gold, out LevelConfigSO config)
        {
            bronze = silver = gold = 0f;
            config = null;
            if (!TryGetLevelConfig(out config) || !config) return false;
            
            bronze = Mathf.Max(0f, config.Bronze.requiredTime);
            silver = Mathf.Max(0f, config.Silver.requiredTime);
            gold   = Mathf.Max(0f, config.Gold.requiredTime);
            return true;
        }

        private void OnDestroy()
        {
            _onLevelWon.Dispose();
            _onGameOver.Dispose();
            _onPlayerDeath.Dispose();
            _onEnemyDeath.Dispose();
            effectPool.Dispose();

            OnEnemyDeathSubject.DetachAll();
            OnEnemySpawnSubject.DetachAll();
            OnProjectileDestroyed.DetachAll();
            OnTimeUpdated.DetachAll();
            
            if (_instance == this)
                _instance = null; // limpiar la referencia estática
        }
    }
}
