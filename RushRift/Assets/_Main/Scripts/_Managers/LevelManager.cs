using System;
using Game.DesignPatterns.Observers;
using Game.VFX;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using System.Collections.Generic;
using Game.UI.Screens;

namespace Game
{
    public enum MedalType { Bronze, Silver, Gold }
    
    /// <summary>
    /// Manages the level state, including win and lose conditions.
    /// </summary>
    [AddComponentMenu("Game/Level Manager")]
    public class LevelManager : MonoBehaviour
    {
        public static readonly ISubject OnEnemyDeathSubject = new Subject();
        public static readonly ISubject OnEnemySpawnSubject = new Subject();
        public static readonly ISubject OnProjectileDestroyed = new Subject();

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
        [SerializeField, Tooltip("Root references for effects and default medal list for this level.")]
        private ScriptableReferenceSO scriptableReference;

        [SerializeField, Tooltip("Explicit medal asset to use for this scene. If set, auto-resolution is skipped.")]
        private LevelMedalsSO levelMedalsOverride;

        [FormerlySerializedAs("vfxPool")]
        [SerializeField, Tooltip("Effect pool for spawning VFX emitters during gameplay.")]
        private EffectPool effectPool;

        [Header("Level Number Resolution")]
        [SerializeField, Tooltip("If true and no override is set, resolves medal by Scene build index + offset.")]
        private bool useSceneBuildIndexForLevelNumber = true;

        [SerializeField, Tooltip("Added to SceneManager.GetActiveScene().buildIndex when resolving level number.")]
        private int buildIndexToLevelOffset = 0;

        [SerializeField, Tooltip("Used as the resolved level number if Use Scene Build Index is false and no override is set.")]
        private int levelNumberOverride = 0;

        [Header("Runtime Flags")]
        [SerializeField, Tooltip("If true, explosive barrels become invulnerable to players when the medal is acquired.")]
        private bool _barrelInvulnerabilityEnabled;

        [Header("Debug Logging")]
        [SerializeField, Tooltip("Enable startup logs of picked medal source and details.")]
        private bool enableStartupLogging = false;

        [SerializeField, Tooltip("If enabled, includes per-tier medal details in the startup log.")]
        private bool verboseMedalLogging = false;

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
            _instance = this;
            _onPlayerDeath = new ActionObserver(OnPlayerDeath);
            _onEnemyDeath = new ActionObserver(OnEnemyDeath);
            FillEffectsDic();
            DumpStartupReferences();
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

        public static List<LevelMedalsSO> GetMedalsList()
        {
            return _instance && _instance.scriptableReference ? _instance.scriptableReference.medalReferences : null;
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

        public static int GetResolvedLevelNumber()
        {
            if (_instance == null) return 0;
            if (_instance.useSceneBuildIndexForLevelNumber)
                return SceneManager.GetActiveScene().buildIndex + _instance.buildIndexToLevelOffset;
            if (_instance.levelNumberOverride > 0) return _instance.levelNumberOverride;
            return SceneManager.GetActiveScene().buildIndex;
        }

        public static bool TryGetActiveMedal(out LevelMedalsSO medal)
        {
            medal = null;
            if (_instance == null) return false;

            if (_instance.levelMedalsOverride)
            {
                medal = _instance.levelMedalsOverride;
                return true;
            }

            if (_instance.scriptableReference == null || _instance.scriptableReference.medalReferences == null) return false;

            int target = GetResolvedLevelNumber();
            var list = _instance.scriptableReference.medalReferences;
            for (int i = 0; i < list.Count; i++)
            {
                var m = list[i];
                if (m != null && m.levelNumber == target) { medal = m; return true; }
            }
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
                Debug.Log("LOG: TryGetVFX: Failure");
#endif
                return false;
            }
        }

        public static MedalInfo GetMedalInfo(MedalType type)
        {
            var data = SaveAndLoad.Load();
            var currLevel = GetCurrentLevel();
            var medal = type switch
            {
                MedalType.Bronze => data.LevelsMedalsTimes[currLevel].bronze,
                MedalType.Silver => data.LevelsMedalsTimes[currLevel].silver,
                MedalType.Gold   => data.LevelsMedalsTimes[currLevel].gold,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
            
            var endTime = LevelCompleteTime();

#if UNITY_EDITOR
            Debug.Log($"LOG: Getting {type} medal [Level: {currLevel} | End Time: {endTime} | Medal Time: {medal.time}]");
#endif
            
            return new MedalInfo(type.ToString(), medal.upgradeText, endTime <= medal.time, medal.isAcquired, medal.time);
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

        private void FillEffectsDic()
        {
            if (!scriptableReference || scriptableReference.effectsReferences == null) return;
            for (int i = 0; i < scriptableReference.effectsReferences.Count; i++)
            {
                var key = scriptableReference.effectsReferences[i].upgradeEnum;
                var value = scriptableReference.effectsReferences[i].effect;
                if (!effectsReferencesDic.ContainsKey(key))
                    effectsReferencesDic.Add(key, value);
            }
        }
        
        public static bool TryGetActiveMedalTimes(out float bronze, out float silver, out float gold, out LevelMedalsSO medal)
        {
            bronze = silver = gold = 0f;
            medal = null;
            if (!TryGetActiveMedal(out medal) || !medal) return false;

            bronze = Mathf.Max(0f, medal.levelMedalTimes.bronze.time);
            silver = Mathf.Max(0f, medal.levelMedalTimes.silver.time);
            gold   = Mathf.Max(0f, medal.levelMedalTimes.gold.time);
            return true;
        }

        
        private void DumpStartupReferences()
        {
            if (!enableStartupLogging) return;

            var scene = SceneManager.GetActiveScene();
            int resolved = GetResolvedLevelNumber();
            string refName = scriptableReference ? scriptableReference.name : "null";
            int effectsCount = scriptableReference && scriptableReference.effectsReferences != null ? scriptableReference.effectsReferences.Count : 0;
            int medalsCount = scriptableReference && scriptableReference.medalReferences != null ? scriptableReference.medalReferences.Count : 0;

            Debug.Log($"[LevelManager] Startup | SceneIndex={scene.buildIndex} SceneName={scene.name} | ResolvedLevel={resolved} | ScriptableReferenceSO={refName} | Effects={effectsCount} | MedalSO Count={medalsCount}", this);

            if (levelMedalsOverride)
            {
                var mt = levelMedalsOverride.levelMedalTimes;
                if (verboseMedalLogging)
                    Debug.Log($"[LevelManager] Medal Source=Override | '{levelMedalsOverride.name}' | levelNumber={levelMedalsOverride.levelNumber} | bronze(time={mt.bronze.time}, acquired={mt.bronze.isAcquired}, upgrade={mt.bronze.upgrade}) | silver(time={mt.silver.time}, acquired={mt.silver.isAcquired}, upgrade={mt.silver.upgrade}) | gold(time={mt.gold.time}, acquired={mt.gold.isAcquired}, upgrade={mt.gold.upgrade})  <-- PICKED", this);
                else
                    Debug.Log($"[LevelManager] Medal Source=Override | '{levelMedalsOverride.name}' | levelNumber={levelMedalsOverride.levelNumber}  <-- PICKED", this);
                return;
            }

            LevelMedalsSO picked = null;
            if (scriptableReference && scriptableReference.medalReferences != null)
            {
                for (int i = 0; i < scriptableReference.medalReferences.Count; i++)
                {
                    var m = scriptableReference.medalReferences[i];
                    if (!m)
                    {
                        Debug.Log($"[LevelManager] MedalSO[{i}] = null", this);
                        continue;
                    }

                    bool isPick = m.levelNumber == resolved;
                    if (isPick) picked = m;

                    if (verboseMedalLogging)
                    {
                        var mt = m.levelMedalTimes;
                        Debug.Log($"[LevelManager] MedalSO[{i}] '{m.name}' | levelNumber={m.levelNumber} | bronze(time={mt.bronze.time}, acquired={mt.bronze.isAcquired}, upgrade={mt.bronze.upgrade}) | silver(time={mt.silver.time}, acquired={mt.silver.isAcquired}, upgrade={mt.silver.upgrade}) | gold(time={mt.gold.time}, acquired={mt.gold.isAcquired}, upgrade={mt.gold.upgrade})" + (isPick ? "  <-- PICKED" : ""), this);
                    }
                    else
                    {
                        Debug.Log($"[LevelManager] MedalSO[{i}] '{m.name}' | levelNumber={m.levelNumber}" + (isPick ? "  <-- PICKED" : ""), this);
                    }
                }
            }

            if (!picked)
                Debug.LogWarning($"[LevelManager] No LevelMedalsSO matched ResolvedLevel={resolved}. Assign an Override to force a specific asset.", this);
            else
                Debug.Log($"[LevelManager] Medal Source=ListMatch | Using '{picked.name}' for ResolvedLevel={resolved}  <-- PICKED", this);
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
        }
    }
}
