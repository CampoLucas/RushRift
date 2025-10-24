using System;
using System.Collections.Generic;
using Tools.Scripts.PropertyAttributes;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Data carrier that defines the current play session.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Levels/Session")]
    public class GameSessionSO : SerializableSO
    {
        public GameModeSO GameMode => gameMode;
        public BaseLevelSO Level => level;
        public int CurrIndex => currentIndex;
        public bool Started => started;
        
        [Header("Definition")]
        [SerializeField] private GameModeSO gameMode;
        [SerializeField] private BaseLevelSO level;
        [SerializeField] private SessionArgs args;

        [Header("Runtime Data")]
        [SerializeField, ReadOnly] private int currentIndex;
        [SerializeField, ReadOnly] private bool started;

        public static GameSessionSO GetOrCreate(NullCheck<GameSessionSO> sessionValue, GameModeSO mode, BaseLevelSO level)
        {
            var session = sessionValue.GetOrDefault(Create);
            session.Initialize(mode, level);

            return session;
        }

        private static GameSessionSO Create()
        {
            return ScriptableObject.CreateInstance<GameSessionSO>();
        }
        
        public void Initialize(GameModeSO mode, BaseLevelSO startLevel)
        {
            gameMode = mode;
            level = startLevel;
            currentIndex = 0;
            started = false;
        }

        public void SetProgress(int index)
        {
            currentIndex = index;
        }

        public void SetLevel(BaseLevelSO newLevel)
        {
            level = newLevel;
        }
    }

    public struct SessionArgs
    {
        
    }
}