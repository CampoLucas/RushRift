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
        [SerializeField] private PlayerController player;
        
        [Header("Presenters")]
        [SerializeField] private GameplayPresenter gameplayPresenter;
        [SerializeField] private GameOverPresenter gameOverPresenter;
        [SerializeField] private PausePresenter pausePresenter;
        [SerializeField] private LevelWonPresenter levelWonPresenter;
        [SerializeField] private OptionsPresenter optionsPresenter;

        [Header("Loading Screen")] // ToDo: make it a state so it fades in
        [SerializeField] private GameObject loadingScreen;
        
        [Header("Effects")]
        [SerializeField] private FadeScreen screenFade;
        [SerializeField] private ScreenDamageEffect screenDamage;

        private static UIManager _instance;
        private NullCheck<UIStateMachine> _stateMachine;
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
            
            CursorHandler.lockState = CursorLockMode.Locked;
            CursorHandler.visible = false;
        }

        private void Start()
        {
            SceneHandler.OnSceneChanged.Attach(_onSceneChanged);
            InitStateMachine();

            gameplayPresenter.Attach(this);
            //gameOverPresenter.Attach(this);
            //levelWonPresenter.Attach(this);
            pausePresenter.Attach(this);
            //optionsPresenter.Attach(this);
            
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
            _stateMachine.Get().Update(Time.deltaTime);
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

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) return;
            if (!_stateMachine.TryGet(out var stateMachine)) return;
            var isGameplay = stateMachine.Current == UIScreen.Gameplay;
            
            CursorHandler.lockState = isGameplay ? CursorLockMode.Locked : CursorLockMode.None;
            CursorHandler.visible = !isGameplay;
        }

        public static bool SetScreen(UIScreen screen, float fadeOutTime = 0, float fadeInTime = 0, float fadeInStartTime = 0)
        {
            if (_instance)
            {
                return _instance._stateMachine.Get().TransitionTo(screen, fadeOutTime, fadeInTime, fadeInStartTime);
            }

            return false;
        }

        private void InitStateMachine()
        {
            if (player == null) return;
            var model = player.GetModel();

            _stateMachine = new UIStateMachine();
            
            if (!_stateMachine.TryGet(out var stateMachine)) return;

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
            
            stateMachine.TransitionTo(UIScreen.Gameplay, 0, .25f, 0);
        }
        
        private void OnGameOverHandler()
        {
            if (!_stateMachine.TryGet(out var stateMachine)) return;
            stateMachine.TransitionTo(UIScreen.GameOver, 1, 2, .75f);
        }
        
        private void OnLevelWonHandler()
        {
            if (!_stateMachine.TryGet(out var stateMachine)) return;
            stateMachine.TransitionTo(UIScreen.LevelWon, 1, 2, .75f);
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

        private void LoadHUB()
        {
            Time.timeScale = 1f;
            SceneHandler.LoadHub();
        }

        private void BackToGameplay()
        {
            Time.timeScale = 1f;
            
            if (!_stateMachine.TryGet(out var stateMachine)) return;
            stateMachine.TransitionTo(UIScreen.Gameplay, .25f, 0, 0);
        }

        private void Restart()
        {
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
            
            if (_stateMachine) _stateMachine.Dispose();
            _onGameOver.Dispose();
            _onLevelWon.Dispose();
            _onHealthChanged.Dispose();

            _onGameOver = null;
            _onLevelWon = null;
            _onHealthChanged = null;
            
            PauseHandler.Dispose();
        }
    }
}