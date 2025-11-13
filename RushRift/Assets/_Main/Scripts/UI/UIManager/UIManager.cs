using System;
using System.Collections.Generic;
using Game.DesignPatterns.Observers;
using Game.Editor;
using Game.Entities.Components;
using Game.InputSystem;
using Game.Levels;
using Game.Predicates;
using Game.ScreenEffects;
using Game.UI.StateMachine;
using Game.UI.StateMachine.Interfaces;
using Game.Utils;
using MyTools.Global;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.UI
{
    public class UIManager : SingletonBehaviour<UIManager>, DesignPatterns.Observers.IObserver<MenuState>
    {
        [Header("Presenters")]
        [SerializeField] private UIStateCollection defaultUI;

        [SerializeField] private Transform presenterParent;
        

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

        private UIStateCollection _currentCollection;

        protected override void OnAwake()
        {
            base.OnAwake();
            
            _onHealthChanged = new ActionObserver<float, float, float>(OnHealthChangedHandler);
            _onGameOver = new ActionObserver<bool>(OnGameOverHandler);
            

            CursorHandler.lockState = CursorLockMode.Locked;
            CursorHandler.visible = false;
            
            Debug.Log("Set Loading screen update");
            loadingScreen.SetActive(true);

            _active = false;
            _onPreload = new ActionObserver<BaseLevelSO>(OnPreloadHandler);
            _onReady = new ActionObserver<BaseLevelSO>(OnReadyHandler);

            GameEntry.LoadingState.AttachOnPreload(_onPreload);
            GameEntry.LoadingState.AttachOnReady(_onReady);
            
            defaultUI.Test();
        }

        private void OnPreloadHandler(BaseLevelSO level)
        {
            _active = false;
            Debug.Log("Add Loading screen");
            if (loadingScreen) loadingScreen.SetActive(true);
            
            
        }
    
        private void OnReadyHandler(BaseLevelSO level)
        {
            Debug.Log("Remove Loading screen");

            if (_onGameOver != null)
            {
                _onGameOver = new ActionObserver<bool>(OnGameOverHandler);
            }
            
            GlobalEvents.GameOver.Attach(_onGameOver);
            if (loadingScreen) loadingScreen.SetActive(false);

            if (_stateMachine.TryGet(out var stateMachine))
            {
                stateMachine.TransitionTo(UIScreen.Gameplay, 0, .25f, 0);
            }

            _active = true;
            if (level == null)
            {
                this.Log("Level is null", LogType.Error);
                return;
            }

            if (level.UI == null || defaultUI == null)
            {
                this.Log($"LevelUI is null {level.UI == null} {defaultUI == null}");
            }
            
            SetUIStateCollection(level.UI ? level.UI : defaultUI);
        }
        
        private void Start()
        {
            //SceneHandler.OnSceneChanged.Attach(_onSceneChanged);
            if (!_stateMachine)
            {
                SetUIStateCollection(defaultUI);
            }

            

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

        public void SetUIStateCollection(UIStateCollection newCollection)
        {
            if (newCollection == null)
            {
                this.Log("Tried to assign a null UIStateCollection.");
                return;
            }

            if (newCollection == _currentCollection)
            {
                return;
            }

            _currentCollection = newCollection;
            RebuildStateMachine(newCollection);
        }
        
        private void RebuildStateMachine(UIStateCollection collection)
        {
            if (!_stateMachine.TryGet(out var stateMachine))
            {
                this.Log("State machine not initialized");
                InitStateMachine(collection);
                return;
            }

            var parent = presenterParent ? presenterParent : transform;
            
            // Destroy all presenter instances from previous collection
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child.IsNullOrMissingReference())
                {
                    this.Log("Child is null or missing", LogType.Error);
                    continue;
                }
                Destroy(child.gameObject);
            }

            // Clear old states and transitions from the FSM
            stateMachine.Clear();

            BuildStateMachine(stateMachine, collection);

            this.Log($"Rebuilt state machine from {_currentCollection.name}");
        }

        private void InitStateMachine(in UIStateCollection collection)
        {
            _stateMachine = new UIStateMachine();
            
            if (!_stateMachine.TryGet(out var stateMachine)) return;
            
            BuildStateMachine(stateMachine, collection);
            // gameplay.AddTransition(UIScreen.Pause, new OnButtonPredicate(InputManager.Input.Pause), 0, .25f, 0);
            // pause.AddTransition(UIScreen.Gameplay, new OnButtonPredicate(InputManager.Input.Pause),.25f, 0, 0);
        }

        private void BuildStateMachine(in UIStateMachine stateMachine,  in UIStateCollection collection)
        {
            foreach (var (screen, presenter) in collection.GetPresenters())
            {
                if (!presenter) continue;
                
                var p = Instantiate(presenter, presenterParent ? presenterParent : transform);
                if (!p) continue;
                p.gameObject.name = $"({screen}){presenter.name}_CLONE";
                
                if (!p.TryGetState(out var state)) continue;
                p.Attach(this);
                stateMachine.TryAddState(screen, state);
            }

            foreach (var trd in collection.GetTransitions())
            {
                if (trd == null) continue;
                trd.SetTransition(stateMachine);
            }

            stateMachine.TransitionTo(collection.GetRootScreen(), 0, 0, 0);
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
            GlobalLevelManager.Restart();
        }

        protected override bool CreateIfNull() => false;
        protected override bool DontDestroy() => false;
        
        protected override void OnDisposeNotInstance()
        {
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