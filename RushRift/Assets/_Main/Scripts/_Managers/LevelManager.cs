using Game.DesignPatterns.Observers;
using Game.VFX;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game
{
    /// <summary>
    /// Manages the level state, including win and lose conditions.
    /// </summary>
    [AddComponentMenu("Game/Level Manager")]
    public class LevelManager : MonoBehaviour
    {
        //[SerializeField] private ScreenManager screenManager;
        [SerializeField] private ScoreManager scoreManager;
        [FormerlySerializedAs("vfxPool")] [SerializeField] private EffectPool effectPool; // Por ahora lo pongo aca para que no sea un singleton

        private static LevelManager _instance;

        private ISubject _onGameOver = new Subject();
        private ISubject _onLevelWon = new Subject();
        private IObserver _onPlayerDeath;
        private IObserver _onEnemyDeath;

        private int _allEnemies;
        private int _deadEnemies;
        private bool _gameOver;
        private bool _gameOverNotified;
        private float _levelCompleteTime;
        
        
        private void Awake()
        {
            _instance = this;
            
            _onPlayerDeath = new ActionObserver(OnPlayerDeath);
            _onEnemyDeath = new ActionObserver(OnEnemyDeath);
        }

        public static void GetPlayerReference(ISubject onDeath)
        {
            if (_instance == null)
            {
                return;
            }

            onDeath.Attach(_instance._onPlayerDeath);
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

        public static float LevelCompleteTime()
        {
            if (_instance) return _instance._levelCompleteTime;
            return 0;
        }

        public static void SetLevelCompleteTime(float time)
        {
            if (_instance) _instance._levelCompleteTime = time;
        }

        public static bool TryGetVFX(string id, VFXEmitterParams vfxParams, out EffectEmitter emitter)
        {
            if (_instance) return _instance.effectPool.TryGetVFX(id, vfxParams, out emitter);
            emitter = null;
            return false;
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

        private void OnDestroy()
        {
            _onLevelWon.Dispose();
            _onGameOver.Dispose();
            _onPlayerDeath.Dispose();
            _onEnemyDeath.Dispose();
            effectPool.Dispose();
        }
    }
}
