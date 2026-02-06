using System;
using Game.DesignPatterns.Observers;
using Game.UI;
using Game.UI.Elements;
using UnityEngine;

namespace Game.LevelSelector
{
    public class LevelSelectorView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LevelSelector controller;
        [SerializeField] private PivotLookAt lookAt;
        
        [Header("Canvas")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private UIAnimationRunner openAnimation;
        [SerializeField] private UIAnimationRunner closeAnimation;
        
        private NullCheck<ActionObserver> _openObs;
        private NullCheck<ActionObserver> _closeObs;

        private void Awake()
        {
            _openObs = new ActionObserver(Open);
            _closeObs = new ActionObserver(Close);
            
            closeAnimation.OnAllSequencesComplete().AddListener(CloseAnimEnded);
            canvas.enabled = false;
        }

        private void Start()
        {
            if (_openObs.TryGet(out var observer))
            {
                controller.Opened.Attach(observer);
            }

            if (_closeObs.TryGet(out observer))
            {
                controller.Closed.Attach(observer);
            }

            if (PlayerSpawner.Player.TryGet(out var player))
            {
                lookAt.SetTarget(player.Origin);
            }
        }

        private void Open()
        {
            closeAnimation.Stop();
            openAnimation.Play();
        }

        private void Close()
        {
            openAnimation.Stop();
            closeAnimation.Play();
        }

        private void CloseAnimEnded()
        {
            canvas.enabled = false;
        }

        private void OnDisable()
        {
            if (_openObs.TryGet(out var observer) && controller.Opened != null)
            {
                controller.Opened.Detach(observer);
            }

            if (_closeObs.TryGet(out observer) && controller.Closed != null)
            {
                controller.Closed.Detach(observer);
            }
            
            _openObs.Dispose();
            _closeObs.Dispose();

            controller = null;
            canvas = null;
            openAnimation = null;
            closeAnimation = null;
        }
    }
}