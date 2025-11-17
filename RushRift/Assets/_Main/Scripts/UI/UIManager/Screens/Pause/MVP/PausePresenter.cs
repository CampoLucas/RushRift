using System;
using _Main.Scripts.Feedbacks;
using Game.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI.StateMachine
{
    public class PausePresenter : UIPresenter<PauseModel, PauseView>
    {
        public bool OnOptions { get; private set; }

        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button optionsBackButton;
        [SerializeField] private Button hubButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Screens")]
        [SerializeField] private Canvas main;
        [SerializeField] private Canvas options;

        public override void Begin()
        {
            base.Begin();
            CursorHandler.lockState = CursorLockMode.None;
            CursorHandler.visible = true;

            EventSystem.current.SetSelectedGameObject(null);
            OnOptionsBackHandler();
        }

        public override void End()
        {
            base.End();
            EventSystem.current.SetSelectedGameObject(null);
        }

        protected override void OnInit()
        {
            base.OnInit();

            
            
            if (resumeButton) resumeButton.onClick.AddListener(OnResumeHandler);
            if (resumeButton) restartButton.onClick.AddListener(OnRestartHandler);
            if (optionsButton) optionsButton.onClick.AddListener(OnOptionsHandler);
            if (optionsBackButton) optionsBackButton.onClick.AddListener(OnOptionsBackHandler);
            if (mainMenuButton) mainMenuButton.onClick.AddListener(OnMainMenuHandler);
            
            if (!hubButton) return;
            if (SceneHandler.GetCurrentSceneName().GetHashCode() == SceneHandler.HubIndex.GetHashCode())
            {
                hubButton.gameObject.SetActive(false);
            }
            else
            {
                hubButton.onClick.AddListener(OnHubHandler);
            }
        }

        private void OnResumeHandler()
        {
            NotifyAll(MenuState.Back);
        }

        private void OnOptionsHandler()
        {
            OnOptions = true;
            main.enabled = false;
            options.enabled = true;
        }

        private void OnOptionsBackHandler()
        {
            OnOptions = false;
            main.enabled = true;
            if (options) options.enabled = false;
        }
        
        private void OnRestartHandler()
        {
            NotifyAll(MenuState.Restart);
        }


        private void OnMainMenuHandler()
        {
            NotifyAll(MenuState.MainMenu);
        }

        private void OnHubHandler()
        {
            NotifyAll(MenuState.HUB);
        }

        public override void Dispose()
        {
            base.Dispose();
            
            if (resumeButton) resumeButton.onClick.RemoveAllListeners();
            if (resumeButton) restartButton.onClick.RemoveAllListeners();
            if (optionsButton) optionsButton.onClick.RemoveAllListeners();
            if (optionsBackButton) optionsBackButton.onClick.RemoveAllListeners();
            if (mainMenuButton) mainMenuButton.onClick.RemoveAllListeners();
            if (hubButton) hubButton.onClick.RemoveAllListeners();
        }
        
        public override bool TryGetState(out UIState state)
        {
            state = new PauseState(this);
            return true;
        }
    }
}