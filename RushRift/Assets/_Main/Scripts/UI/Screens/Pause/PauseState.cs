using Game.Inputs;
using UnityEngine;

namespace Game.UI.Screens
{
    public class PauseState : UIStatePresenter<PausePresenter, PauseModel, PauseView>
    {
        public PauseState(PausePresenter presenter) : base(presenter)
        {
            presenter.Init(new PauseModel());
            
            //AddTransition(UIScreen.Gameplay, new FuncPredicate(OptionClosed));
        }
    }
}