using System;
using Game.DesignPatterns.Observers;
using Game.Entities;
using UnityEngine;
using UnityEngine.Serialization;
using Logger = MyTools.Global.Logger;

namespace Game.UI.Screens
{
    public sealed class GameplayPresenter : UIPresenter<GameplayModel, GameplayView>
    {
        [Header("Attributes")]
        [FormerlySerializedAs("healthBarPresenter")] [SerializeField] private BarBaseUIPresenter healthBarBaseUIPresenter;
        [FormerlySerializedAs("energyBarPresenter")] [SerializeField] private BarBaseUIPresenter energyBarBaseUIPresenter;

        public override void Begin()
        {
            base.Begin();
            // Un Pause the game
            if (!GameEntry.LoadingLevel)
            {
                Debug.Log("[SuperTest] not loading");
                PauseHandler.Pause(false);
            }
            
            // Set cursor
            CursorHandler.lockState = CursorLockMode.Locked;
            CursorHandler.visible = false;
            
            // other presenters
            healthBarBaseUIPresenter.Begin();
            energyBarBaseUIPresenter.Begin();
            
        }

        public override void End()
        {
            base.End();
            // Pause the game
            PauseHandler.Pause(true);
            
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
            if (!PlayerSpawner.Player.TryGet(out var player))
            {
                Logger.Log($"[{typeof(GameplayPresenter)}]: Player not found", logType: LogType.Error);
                state = null;
                return false;
            }
            
            state = new GameplayState(player.GetModel(), this);
            return true;
        }
    }
}