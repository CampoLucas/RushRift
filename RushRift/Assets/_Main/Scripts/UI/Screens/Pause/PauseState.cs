using UnityEngine;

namespace Game.UI.Screens
{
    public class PauseState : UIStatePresenter<PausePresenter, PauseModel, PauseView>
    {
        public PauseState(PausePresenter presenter) : base(presenter)
        {
            presenter.Init(new PauseModel());
        }

        public override void Enable()
        {
            base.Enable();
            Debug.Log("SuperTest: Pause enable");
        }

        public override void Start()
        {
            base.Start();
            Debug.Log("SuperTest: Pause start");
            
        }
    }
}