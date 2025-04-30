using System.Collections;
using System.Collections.Generic;
using Game.DesignPatterns.Observers;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// Manages the level state, including win and lose conditions.
    /// </summary>
    [AddComponentMenu("Game/Level Manager")]
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] private ScreenManager screenManager;

        private static LevelManager _instance;
        
        private ISubject _onGameOver = new Subject();
        private ISubject _onLevelWon = new Subject();
        private IObserver _onPlayerDeath;
        private IObserver _onEnemyDeath;
        
        private int _allEnemies;
        private int _deadEnemies;
        private bool _gameOver;
        private bool _gameOverNotified;
        public ScreenManager ScreenManager => screenManager;
        public static LevelManager Instance => _instance;
        
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

            if (_deadEnemies >= _allEnemies)
            {
                _onLevelWon.NotifyAll();
            }
        }

        private void OnDestroy()
        {
            _onLevelWon.Dispose();
            _onGameOver.Dispose();
            _onPlayerDeath.Dispose();
            _onEnemyDeath.Dispose();
        }
    }
}
