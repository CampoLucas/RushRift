using System;
using System.Collections;
using Game.DesignPatterns.Observers;
using Game.Entities;
using Game.InputSystem;
using Game.Levels;
using Game.UI.StateMachine;
using UnityEngine;

namespace Game.LevelSelector
{
    public class SuperComputer : MonoBehaviour
    {
        public Subject OpenLevelSelector => _openLevelSelector;
        public Subject CloseLevelSelector => _closeLevelSelector;
        public Subject<bool> PlayerInRange => _playerInRangeSubject;
        public bool IsInRange => interactable.IsInRange;
        
        [SerializeField] private Interactable interactable;

        private Subject _openLevelSelector = new();
        private Subject _closeLevelSelector = new();
        private Subject<bool> _playerInRangeSubject = new();
        private ActionObserver _openObs;
        private ActionObserver _closeObs;
        private ActionObserver<bool> _playerInRange;
        private NullCheck<Coroutine> _coroutine;
        private ActionObserver<GameModeSO, BaseLevelSO> _levelSelected;

        private void Awake()
        {
            _openObs = new ActionObserver(OpenLevelSelectorHandler);
            _closeObs = new ActionObserver(CloseLevelSelectorHandler);
            _playerInRange = new ActionObserver<bool>(PlayerInRangeHandler);
            
            interactable.PlayerInteracted.Attach(_openObs);
            interactable.PlayerInRange.Attach(_playerInRange);
            
            _levelSelected = new ActionObserver<GameModeSO, BaseLevelSO>(SetTargetSession);
            LevelSelectorMediator.LevelSelected.Attach(_levelSelected);
        }

        private void Start()
        {
            if (!interactable.SetTarget(GetPlayerTransform()))
            {
                Debug.LogError("[ERROR]: Couldn't assign the player as the target.", gameObject);
            }
        }
        
        public void SetTargetSession(GameModeSO mode, BaseLevelSO level)
        {
            CloseLevelSelectorHandler();
        }

        private void OpenLevelSelectorHandler()
        {
            interactable.PlayerInteracted.Detach(_openObs);
            interactable.PlayerInRange.Detach(_playerInRange);
            interactable.PlayerInteracted.Attach(_closeObs);
            _openLevelSelector.NotifyAll();
        }

        private void CloseLevelSelectorHandler()
        {
            interactable.PlayerInteracted.Detach(_closeObs);
            interactable.PlayerInRange.Attach(_playerInRange);
            interactable.PlayerInteracted.Attach(_openObs);
            _closeLevelSelector.NotifyAll();
        }

        private void PlayerInRangeHandler(bool inRange)
        {
            _playerInRangeSubject.NotifyAll(inRange);
        }

        private Transform GetPlayerTransform()
        {
            return PlayerSpawner.Player.TryGet(out var player) ? player.Origin : null;
        }

        private void OnDestroy()
        {
            _openLevelSelector?.Dispose();
            _openLevelSelector = null;
            
            _closeLevelSelector?.Dispose();
            _closeLevelSelector = null;

            if (interactable && interactable.PlayerInteracted != null)
            {
                interactable.PlayerInteracted.Detach(_openObs);
                interactable.PlayerInteracted.Detach(_closeObs);
            }

            interactable = null;
            
            _openObs?.Dispose();
            _openObs = null;
            
            _closeObs?.Dispose();
            _closeObs = null;
        }
    }
}