using Game.InputSystem;
using UnityEngine;

namespace Game.UI.Screens
{
    public class PauseState : UIState<PausePresenter, PauseModel, PauseView>
    {
        public PauseState(PausePresenter presenter) : base(presenter)
        {
            presenter.Init(new PauseModel());
            
            //AddTransition(UIScreen.Gameplay, new FuncPredicate(OptionClosed));
        }
    }
}