using UnityEngine;

namespace Game.UI.Screens
{
    public sealed class GameOverPresenter : UIPresenter<GameOverModel, GameOverView>
    {
        public override void Begin()
        {
            base.Begin();
            
            // Set Cursor
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }
    }
}