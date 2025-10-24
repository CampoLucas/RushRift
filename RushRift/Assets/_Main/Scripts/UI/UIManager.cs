using System;
using Game.DesignPatterns.Observers;
using Game.Entities.Components;
using Game.Inputs;
using Game.Levels;
using Game.Predicates;
using Game.ScreenEffects;
using Game.UI.Screens;
using Game.Utils;
using MyTools.Global;
using UnityEngine;

namespace Game.UI
{
    public class UIManager : SingletonBehaviour<UIManager>, DesignPatterns.Observers.IObserver<MenuState>
    {
        [Header("Presenters")]
        [SerializeField] private GameplayPresenter gameplayPresenter;
        [SerializeField] private GameOverPresenter gameOverPresenter;
        [SerializeField] private PausePresenter pausePresenter;
        [SerializeField] private LevelWonPresenter levelWonPresenter;
        [SerializeField] private OptionsPresenter optionsPresenter;

        [Header("Loading Screen")] // ToDo: make it a state so it fades in
        [SerializeField] private GameObject loadingScreen;
        
        [Header("Effects")]
        [SerializeField] private ScreenDamageEffect screenDamage;

        [Header("Hub")]
        [SerializeField] private HubSO hubLevel;

        private NullCheck<UIStateMachine> _stateMachine;
        private IObserver<float, float, float> _onHealthChanged;
        private ActionObserver<bool> _onGameOver;

        private bool _active;
        //private IObserver _onSceneChanged;
        
        private ActionObserver<BaseLevelSO> _onPreload;
        private ActionObserver<BaseLevelSO> _onReady;
        private ActionObserver<BaseLevelSO> _initStateMachine;

        protected override void OnAwake()
        {
            base.OnAwake();
            
            _onHealthChanged = new ActionObserver<float, float, float>(OnHealthChangedHandler);
            _onGameOver = new ActionObserver<bool>(OnGameOverHandler);
            //_onSceneChanged = new ActionObserver(OnSceneChangedHandler);

            CursorHandler.lockState = CursorLockMode.Locked;
            CursorHandler.visible = false;
            
            Debug.Log("Set Loading screen update");
            loadingScreen.SetActive(true);

            _active = false;
            _onPreload = new ActionObserver<BaseLevelSO>(OnPreloadHandler);
            _onReady = new ActionObserver<BaseLevelSO>(OnReadyHandler);

            GameEntry.LoadingState.AttachOnPreload(_onPreload);
            GameEntry.LoadingState.AttachOnReady(_onReady);
        }

        private void OnPreloadHandler(BaseLevelSO level)
        {
            _active = false;
            Debug.Log("Add Loading screen");
            loadingScreen.SetActive(true);
        }
    
        private void OnReadyHandler(BaseLevelSO level)
        {
            Debug.Log("Remove Loading screen");
            GlobalEvents.GameOver.Attach(_onGameOver);
            loadingScreen.SetActive(false);

            if (_stateMachine.TryGet(out var stateMachine))
            {
                stateMachine.TransitionTo(UIScreen.Gameplay, 0, .25f, 0);
            }

            _active = true;
        }
        
        private void Start()
        {
            //SceneHandler.OnSceneChanged.Attach(_onSceneChanged);
            
            InitStateMachine();

            gameplayPresenter.Attach(this);
            //gameOverPresenter.Attach(this);
            //levelWonPresenter.Attach(this);
            pausePresenter.Attach(this);
            //optionsPresenter.Attach(this);

            if (!PlayerSpawner.Player.TryGet(out var player))
            {
                this.Log("Player not found", LogType.Error);
                return;
            }
            
            if (player.GetModel() != null && player.GetModel().TryGetComponent<HealthComponent>(out var health))
            {
                health.OnValueChanged.Attach(_onHealthChanged);
            }
        }

        private void Update()
        {
            if (!_active)
            {
                return;
            }
            
            if (!_stateMachine)
            {
                this.Log("The state machine is null", LogType.Error);
                return;
            }
            
            _stateMachine.Get().Update(Time.deltaTime);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) return;
            if (!_stateMachine.TryGet(out var stateMachine)) return;
            var isGameplay = stateMachine.Current == UIScreen.Gameplay;
            
            CursorHandler.lockState = isGameplay ? CursorLockMode.Locked : CursorLockMode.None;
            CursorHandler.visible = !isGameplay;
        }

        private void InitStateMachine()
        {
            if (!PlayerSpawner.Player.TryGet(out var player))
            {
                this.Log("Player not found", LogType.Error);
                return;
            }
            var model = player.GetModel();

            _stateMachine = new UIStateMachine();
            
            if (!_stateMachine.TryGet(out var stateMachine)) return;


#if false
            var gameplay = new GameplayState(model, gameplayPresenter);
            var gameOver = new GameOverState(gameOverPresenter);
            var pause = new PauseState(pausePresenter);
            var levelWon = new LevelWonState(levelWonPresenter);
            
            stateMachine.TryAddState(UIScreen.Gameplay, gameplay);
            stateMachine.TryAddState(UIScreen.GameOver, gameOver);
            stateMachine.TryAddState(UIScreen.Pause, pause);
            stateMachine.TryAddState(UIScreen.LevelWon, levelWon);
            
            gameplay.AddTransition(UIScreen.Pause, new OnButtonPredicate(InputManager.PauseInput), 0, .25f, 0);
            gameplay.AddTransition(SceneTransition.Current, new FuncPredicate(() => UnityEngine.Input.GetKeyDown(KeyCode.R)));
            gameplay.AddTransition(SceneTransition.First, new FuncPredicate(() => UnityEngine.Input.GetKeyDown(KeyCode.H)));
            
            gameOver.AddTransition(SceneTransition.Current, new FuncPredicate(() => UnityEngine.Input.GetKeyDown(KeyCode.R)));
            gameOver.AddTransition(SceneTransition.HUB, new FuncPredicate(() => UnityEngine.Input.GetKeyDown(KeyCode.Escape)));
            
            pause.AddTransition(UIScreen.Gameplay, new OnButtonPredicate(InputManager.PauseInput),.25f, 0, 0);
#else
            if (stateMachine.TryAddState(UIScreen.GameOver, gameOverPresenter, out var gameOver))
            {
                gameOver.AddTransition(SceneTransition.Current, new OnButtonPredicate(InputManager.ResetInput));
                gameOver.AddTransition(SceneTransition.HUB, new OnButtonPredicate(InputManager.PauseInput));
            }
                
            if (stateMachine.TryAddState(UIScreen.Gameplay, gameplayPresenter, out var gameplay))
            {
                gameplay.AddTransition(UIScreen.Pause, new OnButtonPredicate(InputManager.PauseInput), 0, .25f, 0);
                gameplay.AddTransition(SceneTransition.Current, new OnButtonPredicate(InputManager.ResetInput));
            }

            if (stateMachine.TryAddState(UIScreen.Pause, pausePresenter, out var pause))
            {
                pause.AddTransition(UIScreen.Gameplay, new OnButtonPredicate(InputManager.PauseInput),.25f, 0, 0);
            }

            stateMachine.TryAddState(UIScreen.LevelWon, levelWonPresenter, out var levelWon);

#endif




            //stateMachine.TransitionTo(UIScreen.Gameplay, 0, .25f, 0);
        }
        
        private void OnGameOverHandler(bool playerWon)
        {
            GlobalEvents.GameOver.Detach(_onGameOver);
            if (!_stateMachine.TryGet(out var stateMachine)) return;

            if (playerWon)
            {
                stateMachine.TransitionTo(UIScreen.LevelWon, 1, 2, .75f);
            }
            else
            {
                stateMachine.TransitionTo(UIScreen.GameOver, 1, 2, .75f);
            }
        }

        private void OnHealthChangedHandler(float current, float previous, float max)
        {
            if (current < previous) screenDamage.DoEffect(0.1f);
        }

        private void OnDestroy()
        {
            Dispose();
        }

        public void OnNotify(MenuState arg)
        {
            switch (arg)
            {
                case MenuState.MainMenu:
                    LoadMainMenu();
                    break;
                case MenuState.HUB:
                    LoadHUB();
                    break;
                case MenuState.Back:
                    BackToGameplay();
                    break;
                case MenuState.Restart:
                    Restart();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(arg), arg, null);
            }
        }

        private void LoadMainMenu()
        {
            Time.timeScale = 1f;
            SceneHandler.LoadMainMenu();
        }

        public void LoadHUB()
        {
            Time.timeScale = 1f;
            
            // ToDo: Make the GlobalLevelManager have the hub reference and method to load it.
            GameEntry.LoadLevelAsync(hubLevel);
            //SceneHandler.LoadHub();
        }

        private void BackToGameplay()
        {
            Time.timeScale = 1f;
            
            if (!_stateMachine.TryGet(out var stateMachine)) return;
            stateMachine.TransitionTo(UIScreen.Gameplay, .25f, 0, 0);
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            // ToDo: Make the GlobalLevelManager have the method to restart.
            GameEntry.LoadSessionAsync(GlobalLevelManager.CurrentSession);
            //SceneHandler.ReloadCurrent();
        }

        protected override bool CreateIfNull() => false;
        protected override bool DontDestroy() => false;
        
        protected override void OnDisposeNotInstance()
        {
            gameplayPresenter = null;
            gameOverPresenter = null;
            pausePresenter = null;
            levelWonPresenter = null;
            optionsPresenter = null;
            loadingScreen = null;
            screenDamage = null;
        }

        protected override void OnDisposeInstance()
        {
            GameEntry.LoadingState.DetachOnPreload(_onPreload);
            GameEntry.LoadingState.DetachOnReady(_onReady);
            GlobalEvents.GameOver.Detach(_onGameOver);
            
            if (PlayerSpawner.Player.TryGet(out var player))
            {
                var model = player.GetModel();
                if (model != null && model.TryGetComponent<HealthComponent>(out var health))
                {
                    health.OnValueChanged.Detach(_onHealthChanged);
                }
            }
            
            pausePresenter.Detach(this);
            
            if (_stateMachine) _stateMachine.Dispose();
            _onGameOver.Dispose();
            _onHealthChanged.Dispose();
            
            _stateMachine.Dispose();
            _onGameOver.Dispose();
            _onHealthChanged.Dispose();

            _onGameOver = null;
            _onHealthChanged = null;
            
            PauseHandler.Dispose();
        }
    }
}