using System;
using Game.DesignPatterns.Observers;
using Game.Entities;
using Game.Entities.Components;
using Game.Input;
using Game.Inputs;
using Game.ScreenEffects;
using Game.UI.Screens;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.UI
{
    public class UIManager : MonoBehaviour
    {
        public static readonly ISubject OnPaused = new Subject();
        public static readonly ISubject OnUnPaused = new Subject();
        
        [SerializeField] private PlayerController player;
        
        [Header("Views")]
        [SerializeField] private GameplayPresenter gameplayPresenter;
        [SerializeField] private GameOverPresenter gameOverPresenter;
        [SerializeField] private PausePresenter pausePresenter;
        [SerializeField] private LevelWonPresenter levelWonPresenter;

        [Header("Effects")]
        [SerializeField] private FadeScreen screenFade;
        [SerializeField] private ScreenDamageEffect screenDamage;

        private static UIManager _instance;
        private UIStateMachine _stateMachine;
        private IObserver<float, float, float> _onHealthChanged;
        private IObserver _onGameOver;
        private IObserver _onLevelWon;
        
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
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Start()
        {
            InitStateMachine();

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

        // public static bool SetScreen(UIScreen screen)
        // {
        //     if (_instance)
        //     {
        //         return _instance._stateMachine.TryChangeState(screen);
        //     }
        //
        //     return false;
        // }

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
            OnUnPaused.DetachAll();
        }
    }
}