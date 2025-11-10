using UnityEngine;

namespace Game.UI.StateMachine
{
    public sealed class GameOverPresenter : UIPresenter<GameOverModel, GameOverView>
    {
        public override void Begin()
        {
            base.Begin();
            
            // Set Cursor
            CursorHandler.lockState = CursorLockMode.Confined;
            CursorHandler.visible = true;
        }
        
        public override bool TryGetState(out UIState state)
        {
            state = new GameOverState(this);
            return true;
        }
    }
}