using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI.Screens
{
    public class PausePresenter : UIPresenter<PauseModel, PauseView>
    {
        public bool OnOptions { get; private set; }

        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button optionsBackButton;
        [SerializeField] private Button quitButton;

        [Header("Screens")]
        [SerializeField] private Canvas main;
        [SerializeField] private Canvas options;

        [Header("Audio")]
        [SerializeField] private PauseMusicLowPass pauseMusicLowPass;

        public override void Begin()
        {
            base.Begin();

            if (!pauseMusicLowPass)
                pauseMusicLowPass = FindObjectOfType<PauseMusicLowPass>(true);

            pauseMusicLowPass?.SetPaused(true);

            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;

            EventSystem.current.SetSelectedGameObject(null);

            OnOptionsBackHandler();
        }

        public override void End()
        {
            base.End();

            if (!pauseMusicLowPass)
                pauseMusicLowPass = FindObjectOfType<PauseMusicLowPass>(true);

            pauseMusicLowPass?.SetPaused(false);

            EventSystem.current.SetSelectedGameObject(null);
        }

        protected override void OnInit()
        {
            base.OnInit();
            resumeButton.onClick.AddListener(OnResumeHandler);
            restartButton.onClick.AddListener(OnRestartHandler);
            optionsButton.onClick.AddListener(OnOptionsHandler);
            optionsBackButton.onClick.AddListener(OnOptionsBackHandler);
            quitButton.onClick.AddListener(OnQuitHandler);
        }

        private void OnResumeHandler()
        {
            UIManager.SetScreen(UIScreen.Gameplay, .25f, 0, 0);
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
            options.enabled = false;
        }

        private void OnRestartHandler()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OnQuitHandler()
        {
            Application.Quit();
        }

        private void OnDestroy()
        {
            resumeButton.onClick.RemoveAllListeners();
            restartButton.onClick.RemoveAllListeners();
            optionsButton.onClick.RemoveAllListeners();
            optionsBackButton.onClick.RemoveAllListeners();
            quitButton.onClick.RemoveAllListeners();
        }
    }
}