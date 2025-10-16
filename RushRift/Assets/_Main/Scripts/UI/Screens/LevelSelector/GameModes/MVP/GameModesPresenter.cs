using System.Collections.Generic;
using Game.DesignPatterns.Observers;
using Game.General;
using Game.UI.Screens.Elements;
using MyTools.Global;
using UnityEngine;

namespace Game.UI.Screens
{
    public sealed class GameModesPresenter : UIPresenter<GameModesModel, GameModesView>
    {
        [SerializeField] private List<GameModeButton> gameModes;
        
        protected override void OnInit()
        {
            base.OnInit();
            PopulateModes();
        }

        public override bool TryGetState(out UIState state)
        {
            state = new GameModesState(this);
            return true;
        }

        private void PopulateModes()
        {
            for (var i = 0; i < gameModes.Count; i++)
            {
                var gmButton = gameModes[i];

                if (!gmButton)
                {
                    this.Log("GameMode Button is null.", LogType.Error);
                    continue;
                }

                var modeSO = gmButton.Data;
                if (!modeSO)
                {
                    this.Log("GameMode Button's data is null.", LogType.Error);
                    continue;
                }
                
                gmButton.Init(new ActionObserver(() => SelectMode(modeSO)));
            }
        }
        
        private void SelectMode(GameModeSO mode)
        {
            this.Log("Select GameMode");
            
            LevelSelectorMediator.GameModeSelected.NotifyAll(mode);
            NotifyAll(MenuState.Levels);
        }

        public override void Dispose()
        {
            base.Dispose();
            
            gameModes?.Clear();
            gameModes = null;
        }
    }
}