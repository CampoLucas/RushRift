using System;
using System.Collections.Generic;
using Game.DesignPatterns.Observers;
using Game.General;
using Game.Levels;
using Game.Saves;
using Game.UI;
using Game.UI.StateMachine.Elements;
using MyTools.Global;
using UnityEngine;

namespace Game.LevelSelector
{
    public class LevelSelector : MonoBehaviour
    {
        public Subject Opened { get; private set; } = new();
        public Subject Closed { get; private set; } = new();
        
        [SerializeField] private PortalPrototype portal;
        [SerializeField] private SuperComputer computer;
        
        [Header("Game Mode")]
        [SerializeField] private Canvas gameModeCanvas;
        [SerializeField] private List<GameModeButton> gameModes;
        
        [Header("Level")]
        [SerializeField] private Canvas levelCanvas;
        [SerializeField] private LevelButton levelButtonPrefab;
        [SerializeField] private Transform levelContainer;

        [Header("Canvas")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private UIAnimationRunner openAnimation;
        [SerializeField] private UIAnimationRunner closeAnimation;

        private List<LevelButton> _spawnedLevelButtons = new();
        private NullCheck<GameModeSO> _currentMode;
        private NullCheck<BaseLevelSO> _currentLevel;
        private NullCheck<ActionObserver> _openObserver;
        private NullCheck<ActionObserver> _closeObserver;

        private void Awake()
        {
            _openObserver = new ActionObserver(Open);
            _closeObserver = new ActionObserver(Close);
        }

        private void Start()
        {
            if (_openObserver.TryGet(out var observer))
            {
                computer.OpenLevelSelector.Attach(observer);
            }
            
            PopulateModes();
        }

        private void Open()
        {
            // Detach form the open subject
            if (_openObserver.TryGet(out var observer))
            {
                computer.OpenLevelSelector.Detach(observer);
            }

            // Attach to the close subject
            if (_closeObserver.TryGet(out observer))
            {
                computer.CloseLevelSelector.Attach(observer);
            }
            
            Opened.NotifyAll();
        }

        private void Close()
        {
            // Detach form the close subject
            if (_closeObserver.TryGet(out var observer))
            {
                computer.CloseLevelSelector.Detach(observer);
            }

            // Attach to the open subject
            if (_openObserver.TryGet(out observer))
            {
                computer.OpenLevelSelector.Attach(observer);
            }
            
            Closed.NotifyAll();
        }

        private void PopulateModes()
        {
            levelCanvas.enabled = false;
            gameModeCanvas.enabled = true;
        }

        public void BackToModeSelection()
        {
            levelCanvas.enabled = false;
            gameModeCanvas.enabled = true;
        }

        private void OnDestroy()
        {
            Opened?.Dispose();
            Opened = null;
            
            Closed?.Dispose();
            Closed = null;

            portal = null;
            computer = null;
        }
    }
}