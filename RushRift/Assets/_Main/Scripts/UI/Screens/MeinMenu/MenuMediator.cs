using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.UI.Screens
{
    public enum MenuState
    {
        NewGame, Continue, Options, Credits, Quit, Back, MainMenu, HUB, Restart
    }

    /// <summary>
    /// Mediator class that handles the different menus in the Main Menu
    /// </summary>
    public class MenuMediator : MonoBehaviour, DesignPatterns.Observers.IObserver<MenuState>
    {
        [Header("Presenters")]
        [SerializeField] private MainMenuPresenter mainMenuPresenter;
        [SerializeField] private OptionsPresenter optionsMenuPresenter;
        [SerializeField] private CreditsPresenter creditsPresenter;

        [Header("Screen Transitions")]
        [SerializeField] private float fadeOut;
        [SerializeField] private float fadeIn;
        [SerializeField] private float fadeInStart;
        

        private UIStateMachine _stateMachine;
        
        private void Awake()
        {
            mainMenuPresenter.Attach(this);
            optionsMenuPresenter.Attach(this);
            creditsPresenter.Attach(this);
        }

        private void Start()
        {
            InitStateMachine();
        }

        private void Update()
        {
            _stateMachine.Update(Time.deltaTime);
        }

        private void InitStateMachine()
        {
            _stateMachine = new UIStateMachine();

            var mainMenuState = new MainMenuState(mainMenuPresenter);
            var optionsState = new OptionsMenuState(optionsMenuPresenter);
            var creditsState = new CreditsState(creditsPresenter);
            _stateMachine.TryAddState(UIScreen.MainMenu, mainMenuState);
            _stateMachine.TryAddState(UIScreen.Options, optionsState);
            _stateMachine.TryAddState(UIScreen.Credits, creditsState);

            _stateMachine.TransitionTo(UIScreen.MainMenu, 0, 0, 0);
        }

        private void NewGame()
        {
            SaveSystem.ResetGame();

            SceneManager.LoadScene(UIManager.FirstLevelIndex);
            // creates new save and takes you to the first level.
        }

        private void Continue()
        {
            // ToDo: go to the last scene the player was playing
            SceneManager.LoadScene(UIManager.HubIndex);
        }

        private void Options()
        {
            // Opens the options menu.
            _stateMachine.TransitionTo(UIScreen.Options, fadeOut, fadeIn, fadeInStart);
        }

        private void Credits()
        {
            // Opens the credits window
            _stateMachine.TransitionTo(UIScreen.Credits, fadeOut, fadeIn, fadeInStart);
        }

        private void Quit()
        {
            Application.Quit();
        }

        private void MainMenu()
        {
            _stateMachine.TransitionTo(UIScreen.MainMenu, fadeOut, fadeIn, fadeInStart);
        }

        public void OnNotify(MenuState arg)
        {
            switch (arg)
            {
                case MenuState.NewGame:
                    NewGame();
                    break;
                case MenuState.Continue:
                    Continue();
                    break;
                case MenuState.Options:
                    Options();
                    break;
                case MenuState.Credits:
                    Credits();
                    break;
                case MenuState.Quit:
                    Quit();
                    break;
                case MenuState.Back:
                    MainMenu();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(arg), arg, null);
            }
        }

        public void Dispose()
        {
            _stateMachine.Dispose();
            
            mainMenuPresenter.Detach(this);
            optionsMenuPresenter.Detach(this);
            creditsPresenter.Detach(this);
            
            mainMenuPresenter = null;
            optionsMenuPresenter = null;
            creditsPresenter = null;
            _stateMachine = null;
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }

}