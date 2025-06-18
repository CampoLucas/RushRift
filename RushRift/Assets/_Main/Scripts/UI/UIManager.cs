using System;
using Game.DesignPatterns.Observers;
using Game.Entities;
using Game.Entities.Components;
using Game.Input;
using Game.Inputs;
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

        private static UIManager _instance;
        private UIStateMachine _stateMachine;
        private IObserver _onGameOver;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            _onGameOver = new ActionObserver(OnGameOverHandler);
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Start()
        {
            InitStateMachine();

            if (LevelManager.TryGetGameOver(out var subject))
            {
                subject.Attach(_onGameOver);
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
            
            _stateMachine.TryAddState(UIScreen.Gameplay, gameplay);
            _stateMachine.TryAddState(UIScreen.GameOver, gameOver);
            _stateMachine.TryAddState(UIScreen.Pause, pause);
            
            gameplay.AddTransition(UIScreen.Pause, new OnButtonPredicate(InputManager.PauseInput), 0, .25f, 0);
            pause.AddTransition(UIScreen.Gameplay, new OnButtonPredicate(InputManager.PauseInput),.25f, 0, 0);

            _stateMachine.TransitionTo(UIScreen.Gameplay, 0, .25f, 0);
        }
        
        private void OnGameOverHandler()
        {
            _stateMachine.TransitionTo(UIScreen.GameOver, 1, 2, .75f);
            //LevelManager.Instance.ScreenManager.PushScreen(ScreenName.GameOver);
        }

        private void OnDestroy()
        {
            gameplayPresenter = null;
            gameOverPresenter = null;
            pausePresenter = null;
            player = null;
            
            if (_stateMachine != null) _stateMachine.Dispose();
            _onGameOver.Dispose();
            
            
            OnPaused.DetachAll();
            OnUnPaused.DetachAll();
        }
    }
}