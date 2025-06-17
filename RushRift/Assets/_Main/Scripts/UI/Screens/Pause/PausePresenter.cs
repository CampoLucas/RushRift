using UnityEngine;

namespace Game.UI.Screens
{
    public class PausePresenter : UIPresenter<PauseModel, PauseView>
    {
        public override void Begin()
        {
            base.Begin();
            
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }
    }
}