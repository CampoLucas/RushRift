using System;
using Game.DesignPatterns.Observers;
using UnityEngine;
using UnityEngine.Events;

namespace Game.LevelSelector
{
    public class SuperComputerView : MonoBehaviour
    {
        [SerializeField] private SuperComputer controller;

        [Header("View Events")]
        [SerializeField] private UnityEvent levelSelectorOpened;
        [SerializeField] private UnityEvent levelSelectorClosed;
        [SerializeField] private UnityEvent playerInRange;
        [SerializeField] private UnityEvent playerOutOfRange;
        
        private ActionObserver _openObserver;
        private ActionObserver _closedObserver;
        private ActionObserver<bool> _inRangeObserver;

        private void Awake()
        {
            _openObserver = new ActionObserver(OpenHandler);
            _closedObserver = new ActionObserver(ClosedHandler);
            _inRangeObserver = new ActionObserver<bool>(InRangeHandler);
        }

        private void Start()
        {
            controller.OpenLevelSelector.Attach(_openObserver);
            controller.CloseLevelSelector.Attach(_closedObserver);
            controller.PlayerInRange.Attach(_inRangeObserver);
        }

        private void OpenHandler()
        {
            levelSelectorOpened.Invoke();
        }

        private void ClosedHandler()
        {
            levelSelectorClosed.Invoke();
        }

        private void InRangeHandler(bool inRange)
        {
            if (inRange)
            {
                playerInRange.Invoke();
            }
            else
            {
                playerOutOfRange.Invoke();
            }
        }

        private void OnDestroy()
        {
            controller.OpenLevelSelector?.Detach(_openObserver);
            controller.CloseLevelSelector?.Detach(_closedObserver);
            controller.PlayerInRange?.Detach(_inRangeObserver);
            
            _openObserver.Dispose();
            _openObserver = null;
            
            _closedObserver.Dispose();
            _closedObserver = null;
            
            _inRangeObserver.Dispose();
            _inRangeObserver = null;
        }
    }
}