using System;
using Game.DesignPatterns.Observers;
using Game.Entities;
using Game.Entities.Components;
using Game.UI.Screens;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.UI
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        
        [Header("Views")]
        [SerializeField] private GameplayView gameplayView;
        [SerializeField] private GameOverView gameOverView;
        
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
            _stateMachine.TryAddState(UIScreen.Gameplay, new GameplayState(model, gameplayView));
            _stateMachine.TryAddState(UIScreen.GameOver, new GameOverState(gameOverView));

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

            if (Input.GetKeyDown(KeyCode.R))
            {
                // restart scene
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }

            _stateMachine.Update(Time.deltaTime);
        }
        
        private void OnGameOverHandler()
        {
            _stateMachine.TransitionTo(UIScreen.GameOver, 1, 2, .75f);
            //LevelManager.Instance.ScreenManager.PushScreen(ScreenName.GameOver);
        }

        private void OnDestroy()
        {
            gameplayView = null;
            gameOverView = null;
            player = null;
            
            if (_stateMachine != null) _stateMachine.Dispose();
            _onGameOver.Dispose();
        }
    }
}