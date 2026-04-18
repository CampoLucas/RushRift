using System.Collections.Generic;
using Game.DesignPatterns.Observers;
using Game.Levels;
using Game.LevelSelector;
using Game.UI.Mediator;
using Game.UI.StateMachine;
using Game.UI.StateMachine.Elements;
using UnityEngine;

namespace Game.UI.Elements.LevelSelector
{
    public class LevelSelector : MonoBehaviour
    {
        public Subject Opened { get; private set; } = new();
        public Subject Closed { get; private set; } = new();
        
        [SerializeField] private SuperComputer computer;

        [Header("Level")]
        [SerializeField] private UIMediator mediator;

        private ISubject<MenuState> _onMenuChanged = new Subject<MenuState>();
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

            _onMenuChanged.Attach(mediator);
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
            
            _onMenuChanged.NotifyAll(MenuState.GameModes);
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
            
            _onMenuChanged.NotifyAll(MenuState.Interact);
            Closed.NotifyAll();
        }

        private void PopulateModes()
        {
            
        }

        public void BackToModeSelection()
        {
            
        }

        private void OnDestroy()
        {
            Opened?.Dispose();
            Opened = null;
            
            Closed?.Dispose();
            Closed = null;
            computer = null;
        }
    }
}