
using System;
using Game.DesignPatterns.Observers;
using Game.Levels;
using Game.Saves;
using Game.Utils;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.UI.Screens
{
    public enum MenuState
    {
        NewGame, Continue, Options, Credits, Quit, Back, MainMenu, HUB, Restart, GameModes, Levels
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
        
        [Header("Loading Screen")] // ToDo: make it a state so it fades in
        [SerializeField] private GameObject loadingScreen;
        

        [Header("Screen Transitions")]
        [SerializeField] private float fadeOut;
        [SerializeField] private float fadeIn;
        [SerializeField] private float fadeInStart;

        [Header("Levels")]
        [SerializeField] private HubSO hub;
        [SerializeField] private GameModeSO defaultGameMode;
        

        private UIStateMachine _stateMachine;
        private IObserver _onSceneChanged;
        private bool _loadingLevel;
        
        private void Awake()
        {
            mainMenuPresenter.Attach(this);
            optionsMenuPresenter.Attach(this);
            creditsPresenter.Attach(this);
            
            _onSceneChanged = new ActionObserver(OnSceneChangedHandler);
        }

        private void Start()
        {
            SceneHandler.OnSceneChanged.Attach(_onSceneChanged);
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

        private void OnSceneChangedHandler()
        {
            if (_onSceneChanged != null)
            {
                SceneHandler.OnSceneChanged.Detach(_onSceneChanged);
                _onSceneChanged.Dispose();
                _onSceneChanged = null;
            }

            if (loadingScreen) loadingScreen.SetActive(true);
        }

        private void NewGame()
        {
            if (_loadingLevel) return;
            _loadingLevel = true;
            SaveSystem.ResetGame();

            var session = GameSessionSO.GetOrCreate(GlobalLevelManager.CurrentSession, defaultGameMode, defaultGameMode.Levels[0]);
            
            
            GameEntry.LoadSessionAsync(session);
            //SceneHandler.LoadFirstLevel();
            // creates new save and takes you to the first level.
        }

        private void Continue()
        {
            if (_loadingLevel) return;
            _loadingLevel = true;
            // ToDo: go to the last scene the player was playing
            //SceneHandler.LoadLastLevel();
            GameEntry.LoadLevelAsync(hub);
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
            if (_onSceneChanged != null)
            {
                SceneHandler.OnSceneChanged.Detach(_onSceneChanged);
                _onSceneChanged.Dispose();
                _onSceneChanged = null;
            }
            
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