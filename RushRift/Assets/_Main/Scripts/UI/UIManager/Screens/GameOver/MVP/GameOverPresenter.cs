using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.StateMachine
{
    public sealed class GameOverPresenter : UIPresenter<GameOverModel, GameOverView>
    {
        [Header("Buttons")]
        [SerializeField] private Button retryButton;
        [SerializeField] private Button hubButton;
        
        private void Awake()
        {
            if (retryButton) retryButton.onClick.AddListener(RetryLevelHandler);
            if (hubButton) hubButton.onClick.AddListener(HubHandler);
        }
        
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
        
        private void HubHandler()
        {
            UIManager.Instance.Get().LoadHUB();
        }

        private void RetryLevelHandler()
        {
            UIManager.Instance.Get().Restart();
        }
    }
}