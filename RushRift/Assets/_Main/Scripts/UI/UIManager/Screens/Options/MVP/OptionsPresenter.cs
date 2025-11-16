using Game.DesignPatterns.Observers;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.StateMachine
{
    public class OptionsPresenter : UIPresenter<OptionsModel, OptionsView>
    {
        [Header("Buttons")]
        [SerializeField] private Button backButton;

        private void Start()
        {
            backButton.onClick.AddListener(OnBackHandler);
        }

        private void OnBackHandler()
        {
            NotifyAll(MenuState.Back);
        }
        
        public override void Dispose()
        {
            backButton.onClick.RemoveAllListeners();
            
            base.Dispose();
        }
        
        public override bool TryGetState(out UIState state)
        {
            state = new OptionsMenuState(this);
            return true;
        }
    }
}