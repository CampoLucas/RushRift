using System;
using Game.DesignPatterns.Observers;
using Game.Entities;
using Game.Entities.Components;
using Game.Input;
using Game.Inputs;
using Game.ScreenEffects;
using Game.UI.Screens;
using Game.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Game.UI
{
    public class UIManager : MonoBehaviour, DesignPatterns.Observers.IObserver<MenuState>
    {
        public static readonly ISubject OnPaused = new Subject();
        public static readonly ISubject OnUnpaused = new Subject();
        
        [SerializeField] private PlayerController player;
        
        [Header("Presenters")]
        [SerializeField] private GameplayPresenter gameplayPresenter;
        [SerializeField] private GameOverPresenter gameOverPresenter;
        [SerializeField] private PausePresenter pausePresenter;
        [SerializeField] private LevelWonPresenter levelWonPresenter;

        [Header("Loading Screen")] // ToDo: make it a state so it fades in
        [SerializeField] private GameObject loadingScreen;
        
        [Header("Effects")]
        [SerializeField] private FadeScreen screenFade;
        [SerializeField] private ScreenDamageEffect screenDamage;

        private static UIManager _instance;
        private UIStateMachine _stateMachine;
        private IObserver<float, float, float> _onHealthChanged;
        private IObserver _onGameOver;
        private IObserver _onLevelWon;
        private IObserver _onSceneChanged;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            _onHealthChanged = new ActionObserver<float, float, float>(OnHealthChangedHandler);
            _onGameOver = new ActionObserver(OnGameOverHandler);
            _onLevelWon = new ActionObserver(OnLevelWonHandler);
            _onSceneChanged = new ActionObserver(OnSceneChangedHandler);
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Start()
        {
            SceneHandler.OnSceneChanged.Attach(_onSceneChanged);
            InitStateMachine();

            pausePresenter.Attach(this);
            
            if (LevelManager.TryGetGameOver(out var gameOverSubject))
            {
                gameOverSubject.Attach(_onGameOver);
            }

            if (LevelManager.TryGetLevelWon(out var levelWonSubject))
            {
                levelWonSubject.Attach(_onLevelWon);
            }

            if (player.GetModel().TryGetComponent<HealthComponent>(out var health))
            {
                health.OnValueChanged.Attach(_onHealthChanged);
            }
        }

        private void Update()
        {
            _stateMachine.Update(Time.deltaTime);
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

        public static bool SetScreen(UIScreen screen, float fadeOutTime = 0, float fadeInTime = 0, float fadeInStartTime = 0)
        {
            if (_instance)
            {
                return _instance._stateMachine.TransitionTo(screen, fadeOutTime, fadeInTime, fadeInStartTime);
            }

            return false;
        }

        private void InitStateMachine()
        {
            if (player == null) return;
            var model = player.GetModel();

            _stateMachine = new UIStateMachine();

            var gameplay = new GameplayState(model, gameplayPresenter);
            var gameOver = new GameOverState(gameOverPresenter);
            var pause = new PauseState(pausePresenter);
            var levelWon = new LevelWonState(levelWonPresenter);
            
            _stateMachine.TryAddState(UIScreen.Gameplay, gameplay);
            _stateMachine.TryAddState(UIScreen.GameOver, gameOver);
            _stateMachine.TryAddState(UIScreen.Pause, pause);
            _stateMachine.TryAddState(UIScreen.LevelWon, levelWon);
            
            gameplay.AddTransition(UIScreen.Pause, new OnButtonPredicate(InputManager.PauseInput), 0, .25f, 0);
            gameplay.AddTransition(SceneTransition.Current, new FuncPredicate(() => UnityEngine.Input.GetKeyDown(KeyCode.R)));
            gameplay.AddTransition(SceneTransition.First, new FuncPredicate(() => UnityEngine.Input.GetKeyDown(KeyCode.H)));
            
            gameOver.AddTransition(SceneTransition.Current, new FuncPredicate(() => UnityEngine.Input.GetKeyDown(KeyCode.R)));
            gameOver.AddTransition(SceneTransition.First, new FuncPredicate(() => UnityEngine.Input.GetKeyDown(KeyCode.Escape)));
            
            pause.AddTransition(UIScreen.Gameplay, new OnButtonPredicate(InputManager.PauseInput),.25f, 0, 0);
            
            _stateMachine.TransitionTo(UIScreen.Gameplay, 0, .25f, 0);
        }
        
        private void OnGameOverHandler()
        {
            _stateMachine.TransitionTo(UIScreen.GameOver, 1, 2, .75f);
        }
        
        private void OnLevelWonHandler()
        {
            _stateMachine.TransitionTo(UIScreen.LevelWon, 1, 2, .75f);
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
            PauseEventBus.SetPaused(false);
            Time.timeScale = 1f;
            SceneHandler.LoadMainMenu();
        }

        private void LoadHUB()
        {
            PauseEventBus.SetPaused(false);
            Time.timeScale = 1f;
            SceneHandler.LoadHub();
        }

        private void BackToGameplay()
        {
            PauseEventBus.SetPaused(false);
            Time.timeScale = 1f;
            _stateMachine.TransitionTo(UIScreen.Gameplay, .25f, 0, 0);
        }

        private void Restart()
        {
            PauseEventBus.SetPaused(false);
            Time.timeScale = 1f;
            SceneHandler.ReloadCurrent();
        }

        public void Dispose()
        {
            if (_onSceneChanged != null)
            {
                SceneHandler.OnSceneChanged.Detach(_onSceneChanged);
                _onSceneChanged.Dispose();
                _onSceneChanged = null;
            }
            
            if (LevelManager.TryGetGameOver(out var gameOverSubject))
            {
                gameOverSubject.Detach(_onGameOver);
            }

            if (LevelManager.TryGetLevelWon(out var levelWonSubject))
            {
                levelWonSubject.Detach(_onLevelWon);
            }

            var model = player.GetModel();
            if (model != null && model.TryGetComponent<HealthComponent>(out var health))
            {
                health.OnValueChanged.Detach(_onHealthChanged);
            }
            
            pausePresenter.Detach(this);
            
            gameplayPresenter = null;
            gameOverPresenter = null;
            pausePresenter = null;
            player = null;
            
            if (_stateMachine != null) _stateMachine.Dispose();
            _onGameOver.Dispose();
            _onLevelWon.Dispose();
            _onHealthChanged.Dispose();

            _onGameOver = null;
            _onLevelWon = null;
            _onHealthChanged = null;
            
            
            OnPaused.DetachAll();
            OnUnpaused.DetachAll();
        }
    }
}