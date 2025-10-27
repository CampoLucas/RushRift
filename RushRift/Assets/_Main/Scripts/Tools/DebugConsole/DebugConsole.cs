using System;
using System.Collections;
using System.Collections.Generic;
using Game.Entities;
using Game.Entities.Components;
using Game.InputSystem;
using Game.UI;
using MyTools.Global;
using Unity.VisualScripting;
using UnityEngine;
using InputAction = UnityEngine.InputSystem.InputAction;

namespace Game.Tools.DebugCommands
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Game/Debug Console")]

    public class DebugConsole : MonoBehaviour
    {
        private const float ConsoleHeight = 30f;

        private InputManager _inputManager;
        private PlayerControls _playerControls;
        private bool _showConsole;
        private string _input;
        private CursorLockMode _mode;
        private bool _isVisible;

        public static DebugCommand KILL_ALL;
        public static DebugCommand GOD_MODE; // Unlimited health, unlimited stamina

        public List<object> commandList;

        private void Awake()
        {
            KILL_ALL = new DebugCommand("kill_all", "Removes all the enemies from the scene.", "kill_all", () =>
            {
                var controllers = new List<IController>();
                controllers.AddRange(FindObjectsOfType<EnemyController>());
                controllers.AddRange(FindObjectsOfType<LaserController>());

                for (var i = 0; i < controllers.Count; i++)
                {
                    var e = controllers[i];
                    
                    if (e == null) continue;
                    var model = e.GetModel();
                    
                    if (model == null) continue;
                    if (model.TryGetComponent<DestroyableComponent>(out var destroyableComponent))
                    {
                        destroyableComponent.DestroyEntity();
                    }
                    else if (model.TryGetComponent<HealthComponent>(out var healthComponent))
                    {
                        healthComponent.Intakill(Vector3.zero);
                    }
                    else
                    {
                        this.Log($"Couldn't remove enemy {e.GetType()}", LogType.Warning);
                    }
                }
            });

            commandList = new List<object>
            {
                KILL_ALL
            };

            _inputManager = GetComponent<InputManager>();
        }

        private void OnEnable()
        {
            if (_playerControls == null) _playerControls = new();
            _playerControls.Enable();

            _playerControls.Console.ToggleDebug.performed += OnToggleDebug;
        }

        

        private void OnDisable()
        {
            if (_playerControls == null) return;

            _playerControls.Console.ToggleDebug.performed -= OnToggleDebug;
            _playerControls.Console.Return.performed -= OnReturn;
            _playerControls.Disable();
        }

        private void OnToggleDebug(InputAction.CallbackContext obj)
        {
            _showConsole = !_showConsole;

            if (_showConsole)
            {
                EnableDebug();
            }
            else
            {
                DisableDebug();
            }
        }

        private void EnableDebug()
        {
            _mode = CursorHandler.lockState;
            _isVisible = CursorHandler.visible;
                
            if (_inputManager)
            {
                _inputManager.enabled = false;
            }

            CursorHandler.lockState = CursorLockMode.None;
            CursorHandler.visible = true;
            
            _playerControls.Console.Return.performed += OnReturn;
            _playerControls.Console.Close.performed += OnClose;
        }

        private void DisableDebug()
        {
            if (_inputManager)
            {
                _inputManager.enabled = true;
            }

            CursorHandler.lockState = _mode;
            CursorHandler.visible = _isVisible;
            
            _playerControls.Console.Return.performed -= OnReturn;
            _playerControls.Console.Close.performed -= OnClose;
        }

        private void OnReturn(InputAction.CallbackContext obj)
        {
            HandleInput();
            this.Log($"[{_input}]");
            _input = "";
        }
        
        private void OnClose(InputAction.CallbackContext obj)
        {
            _showConsole = false;
            DisableDebug();
        }

        private void OnGUI()
        {
            if (!_showConsole)
            {
                return;
            }

            var y = Screen.height - ConsoleHeight;
            var width = (float)Screen.width / 2;
            GUI.Box(new Rect(0, y, width, ConsoleHeight), "");
            _input = GUI.TextField(new Rect(10f, y - 5f, width - 20f, 20f), _input);
        }

        private void HandleInput()
        {
            for (var i = 0; i < commandList.Count; i++)
            {
                if (commandList[i] is not DebugCommandBase command || !_input.Contains(command.ID)) continue;

                if (command is DebugCommand c)
                {
                    c.Do();
                }
            }
        }
    }
}
