using System;
using Game.DesignPatterns.Observers;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.UI.Screens
{
    public class GameplayPresenter : UIPresenter<GameplayModel, GameplayView>
    {
        [Header("Attributes")]
        [SerializeField] private BarPresenter healthBarPresenter;
        [SerializeField] private BarPresenter energyBarPresenter;

        public override void Begin()
        {
            base.Begin();
            // Un Pause the game
            UIManager.OnUnPaused.NotifyAll();
            
            // Set cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            // other presenters
            healthBarPresenter.Begin();
            energyBarPresenter.Begin();
            
        }

        public override void End()
        {
            base.End();
            // Pause the game
            UIManager.OnPaused.NotifyAll();
            
            // other presenters
            healthBarPresenter.End();
            energyBarPresenter.End();
        }

        public override void Dispose()
        {
            healthBarPresenter.Dispose();
            healthBarPresenter = null;
            
            energyBarPresenter.Dispose();
            energyBarPresenter = null;
            
            base.Dispose();
        }

        protected override void OnInit()
        {
            base.OnInit();
            healthBarPresenter.Init(Model.HealthBar);
            energyBarPresenter.Init(Model.EnergyBar);
        }
    }
}