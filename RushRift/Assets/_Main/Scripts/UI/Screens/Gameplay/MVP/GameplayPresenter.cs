using System;
using Game.DesignPatterns.Observers;
using Game.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.UI.Screens
{
    public sealed class GameplayPresenter : UIPresenter<GameplayModel, GameplayView>
    {
        [FormerlySerializedAs("healthBarPresenter")]
        [Header("Attributes")]
        [SerializeField] private BarBaseUIPresenter healthBarBaseUIPresenter;
        [FormerlySerializedAs("energyBarPresenter")] [SerializeField] private BarBaseUIPresenter energyBarBaseUIPresenter;

        public override void Begin()
        {
            base.Begin();
            // Un Pause the game
            UIManager.OnUnpaused.NotifyAll();
            
            // Set cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            // other presenters
            healthBarBaseUIPresenter.Begin();
            energyBarBaseUIPresenter.Begin();
            
        }

        public override void End()
        {
            base.End();
            // Pause the game
            UIManager.OnPaused.NotifyAll();
            
            // other presenters
            healthBarBaseUIPresenter.End();
            energyBarBaseUIPresenter.End();
        }

        public override void Dispose()
        {
            if (healthBarBaseUIPresenter)
            {
                healthBarBaseUIPresenter.Dispose();
                healthBarBaseUIPresenter = null;
            }

            if (energyBarBaseUIPresenter)
            {
                energyBarBaseUIPresenter.Dispose();
                energyBarBaseUIPresenter = null;
            }
            
            base.Dispose();
        }

        protected override void OnInit()
        {
            base.OnInit();
            healthBarBaseUIPresenter.Init(Model.HealthBar);
            energyBarBaseUIPresenter.Init(Model.EnergyBar);
        }
        
        public override bool TryGetState(out UIState state)
        {
            var playerController = FindObjectOfType<PlayerController>();

            if (!playerController)
            {
                state = null;
                return false;
            }
            
            state = new GameplayState(playerController.GetModel(), this);
            return true;
        }
    }
}