using System;
using Game.Saves;
using Game.UI.StateMachine.Elements;
using Game.DataBase;
using MyTools.Global;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;
using TMPro;

namespace Game.UI.StateMachine
{
    public sealed class MainMenuPresenter : UIPresenter<MainMenuModel, MainMenuView>
    {
        [Header("Buttons")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button quitButton;

        [Header("PopUp")]
        [SerializeField] private PopUp popUp;
        [SerializeField] private GameObject popUpBackground;

        [Header("InputField")]
        [SerializeField] private TMP_InputField username;

        private void Awake()
        {
            popUpBackground.gameObject.SetActive(false);
        }

        private void Start()
        {
            if (newGameButton)
            {
                newGameButton.onClick.AddListener(OnTryNewGameHandler);
            }

            if (continueButton)
            {
                continueButton.onClick.AddListener(OnContinueHandler);
            }

            if (creditsButton)
            {
                creditsButton.onClick.AddListener(OnCreditsHandler);
            }

            if (optionsButton)
            {
                optionsButton.onClick.AddListener(OnOptionsHandler);
            }

            if (quitButton)
            {
                quitButton.onClick.AddListener(OnQuitHandler);
            }

            DataBaseHandler.Init();
        }

        protected override void OnInit()
        {
            base.OnInit();

            if (!SaveSystem.HasSaveGame())
            {
                continueButton.interactable = false;
            }
        }

        private void OnTryNewGameHandler()
        {
            if (SaveSystem.HasSaveGame())
            {
                this.Log("Open pop up");
                popUpBackground.gameObject.SetActive(true);
                popUp.Open(OnNewGameHandler);
            }
            else
            {
                this.Log("Don't open pop up");
                OnNewGameHandler();
            }
        }

        private void OnNewGameHandler()
        {
            NotifyAll(MenuState.NewGame);
        }

        private void OnContinueHandler()
        {
            NotifyAll(MenuState.Continue);
        }

        private void OnCreditsHandler()
        {
            NotifyAll(MenuState.Credits);
        }

        private void OnOptionsHandler()
        {
            NotifyAll(MenuState.Options);
        }

        private void OnQuitHandler()
        {
            NotifyAll(MenuState.Quit);
        }
        
        public override void Dispose()
        {
            if (newGameButton)
            {
                newGameButton.onClick.RemoveAllListeners();
            }

            if (continueButton)
            {
                continueButton.onClick.RemoveAllListeners();
            }

            if (creditsButton)
            {
                creditsButton.onClick.RemoveAllListeners();
            }

            if (optionsButton)
            {
                optionsButton.onClick.RemoveAllListeners();
            }

            if (quitButton)
            {
                quitButton.onClick.RemoveAllListeners();
            }
            
            base.Dispose();
        }
        
        public override bool TryGetState(out UIState state)
        {
            state = new MainMenuState(this);
            return true;
        }

        public void saveUsername()
        {
            string usernameToSend;
            if(username.text != null)
            {
                usernameToSend = username.text;
            }
            else
            {
                usernameToSend = "NoName";
            }

            CancellationTokenSource cts = new CancellationTokenSource();
            var save = SaveSystem.LoadGame();
            save.SetUsername(usernameToSend);
            save.SaveGame();
            DataBaseHandler.DB.SendUsername(username.text, OnReceivedId, cts.Token);
        }

        private void OnReceivedId(int value)
        {
            var save = SaveSystem.LoadGame();
            save.SetUserId(value);
            save.SaveGame();

        }
    }
}