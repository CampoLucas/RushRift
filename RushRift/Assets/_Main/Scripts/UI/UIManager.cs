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
            _onGameOver = new ActionObserver(OnGameOver);
            
            //Cursor.lockState = CursorLockMode.Locked;
            //Cursor.visible = false;
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

            if (model == null || !model.TryGetComponent<HealthComponent>(out var health)) return;
            
            // if (!LevelManager.TryGetAttributeSubjects(out var onHealthChanged, 
            //         out var onStaminaChanged, 
            //         out var onManaChanged)) return;
            //
            // var gameplayModel = new GameplayModel(onHealthChanged, onStaminaChanged, onManaChanged);

            var healthBarData = new AttributeBarData(health.Value, health.MaxValue, health.OnValueChanged);
            
            var gameplayModel = new GameplayModel(healthBarData);
            var gameplayPresenter = new GameplayPresenter(gameplayModel, gameplayView);

            var gameOverModel = new GameOverModel();
            var gameOverPresenter = new GameOverPresenter(gameOverModel, gameOverView);

            _stateMachine = new UIStateMachine();
            _stateMachine.TryAddState(new GameplayState(gameplayPresenter));
            _stateMachine.TryAddState(new GameOverState(gameOverPresenter));

            _stateMachine.TransitionTo<GameplayState>(0, .25f, 0);
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
        
        private void OnGameOver()
        {
            //_stateMachine.TransitionTo<GameOverState>(1, 2, .75f);
            LevelManager.Instance.ScreenManager.PushScreen(ScreenName.GameOver);
        }

        private void OnDestroy()
        {
            gameplayView = null;
            gameOverView = null;
            player = null;
            
            _stateMachine.Dispose();
            _onGameOver.Dispose();
        }
    }
}