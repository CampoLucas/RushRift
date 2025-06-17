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
        
        private UIStateMachine _stateMachine;
        private IObserver _onGameOver;
        
        private void Awake()
        {
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
            
            gameplay.AddTransition(UIScreen.Pause, new OnButtonDownPredicate(InputManager.PauseInput), .5f, .5f, 0);
            pause.AddTransition(UIScreen.Gameplay, new OnButtonDownPredicate(InputManager.PauseInput),.5f, .5f, 0);

            _stateMachine.TransitionTo(UIScreen.Gameplay, 0, .25f, 0);
        }

        private void Update()
        {
            // if (Input.GetKeyDown(KeyCode.G))
            // {
            //     _stateMachine.TransitionTo<GameplayState>(1, 1, .75f);
            // }

            //if (Input.GetKeyDown(KeyCode.Escape))
            //{
            //    Application.Quit();
            //}

            // if (Input.GetKeyDown(KeyCode.R))
            // {
            //     // restart scene
            //     SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            // }

            _stateMachine.Update(Time.deltaTime);
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