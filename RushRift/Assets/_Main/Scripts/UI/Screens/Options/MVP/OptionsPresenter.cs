using Game.DesignPatterns.Observers;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Screens
{
    public class OptionsPresenter : MenuPresenter<OptionsModel, OptionsView>
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
    }
}