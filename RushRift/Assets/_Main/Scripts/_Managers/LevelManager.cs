using Game.DesignPatterns.Observers;
using Game.VFX;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using System.Collections.Generic;


namespace Game
{
    /// <summary>
    /// Manages the level state, including win and lose conditions.
    /// </summary>
    [AddComponentMenu("Game/Level Manager")]
    public class LevelManager : MonoBehaviour
    {
        public static readonly ISubject OnEnemyDeathSubject = new Subject(); // ToDo: Move it to the a EnemyManager and dispose of all references
        public static readonly ISubject OnEnemySpawnSubject = new Subject();
        public static readonly ISubject<int> OnEnemyGivesPoints = new Subject<int>();

        public static bool CanUseTerminal
        {
            get => _instance && _instance._canUseTerminals;
            set { if (_instance) _instance._canUseTerminals = value; }
        }
        
        //[SerializeField] private ScreenManager screenManager;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private ScriptableReferenceSO scriptableReference;
        [FormerlySerializedAs("vfxPool")] [SerializeField] private EffectPool effectPool; // Por ahora lo pongo aca para que no sea un singleton

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


        private void Awake()
        {
            _instance = this;

            _onPlayerDeath = new ActionObserver(OnPlayerDeath);
            _onEnemyDeath = new ActionObserver(OnEnemyDeath);

            FillEffectsDic();
        }

        public static void GetPlayerReference(ISubject onDeath)
        {
            if (_instance == null)
            {
                return;
            }

            onDeath.Attach(_instance._onPlayerDeath);
        }

        public static Entities.Effect GetEffect(UpgradeEnum upgrade)
        {
            return _instance.effectsReferencesDic[upgrade];
        }

        public static List<LevelMedalsSO> GetMedals()
        {
            return _instance.scriptableReference.medalReferences;
        }

        public static void GetEnemiesReference(ISubject onDeath)
        {
            if (_instance == null)
            {
                return;
            }

            onDeath.Attach(_instance._onEnemyDeath);
            _instance._allEnemies += 1;
        }
        
        public static bool TryGetGameOver(out ISubject subject)
        {
            if (_instance == null || _instance._onGameOver == null)
            {
                subject = null;
                return false;
            }

            subject = _instance._onGameOver;
            return true;
        }
        
        public static bool TryGetLevelWon(out ISubject subject)
        {
            if (_instance == null || _instance._onLevelWon == null)
            {
                subject = null;
                return false;
            }

            subject = _instance._onLevelWon;
            return true;
        }

        public static bool IsGameOver()
        {
            if (_instance == null) return true;
            return _instance._gameOver;
        }

        public static int CurrentPoints()
        {
            if (_instance) return _instance.scoreManager.CurrentPoints;
            return 0;
        }

        public static int GetCurrentLevel()
        {
            if (_instance) return SceneManager.GetActiveScene().buildIndex;
            return 0;
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
            if (!_instance)
            {
                emitter = null;
                return false;
            }
            
            //return _instance.effectPool.TryGetVFX(id, vfxParams, out emitter);
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

        private void OnPlayerDeath()
        {
            if (!_gameOverNotified)
            {
                _gameOverNotified = true;
            }
            else
            {
                return;
            }
            _gameOver = true;
            _onGameOver.NotifyAll();
        }
        
        private void OnEnemyDeath()
        {
            _deadEnemies += 1;
        }

        private void FillEffectsDic()
        {
            for (int i = 0; i < _instance.scriptableReference.effectsReferences.Count; i++)
            {
                var key = _instance.scriptableReference.effectsReferences[i].upgradeEnum;
                var value = _instance.scriptableReference.effectsReferences[i].effect;
                effectsReferencesDic.Add(key, value);
            }
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
            OnEnemyGivesPoints.DetachAll();
        }
    }
}
