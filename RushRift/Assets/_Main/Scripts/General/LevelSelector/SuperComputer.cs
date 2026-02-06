using System;
using System.Collections;
using Game.DesignPatterns.Observers;
using Game.Entities;
using Game.InputSystem;
using UnityEngine;

namespace Game.LevelSelector
{
    public class SuperComputer : MonoBehaviour
    {
        public Subject OpenLevelSelector => _openLevelSelector;
        public Subject CloseLevelSelector => _closeLevelSelector;
        
        [SerializeField] private Interactable interactable;

        private Subject _openLevelSelector = new();
        private Subject _closeLevelSelector = new();
        private ActionObserver _openObs;
        private ActionObserver _closeObs;
        private NullCheck<Coroutine> _coroutine;

        private void Awake()
        {
            _openObs = new ActionObserver(OpenLevelSelectorHandler);
            _closeObs = new ActionObserver(CloseLevelSelectorHandler);
            
            interactable.PlayerInteracted.Attach(_openObs);
        }

        private void Start()
        {
            if (!interactable.SetTarget(GetPlayerTransform()))
            {
                Debug.LogError("[ERROR]: Couldn't assign the player as the target.", gameObject);
            }
        }

        private void OpenLevelSelectorHandler()
        {
            interactable.PlayerInteracted.Detach(_openObs);
            interactable.PlayerInteracted.Attach(_closeObs);
            _openLevelSelector.NotifyAll();
        }

        private void CloseLevelSelectorHandler()
        {
            interactable.PlayerInteracted.Detach(_closeObs);
            interactable.PlayerInteracted.Attach(_openObs);
            _closeLevelSelector.NotifyAll();
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