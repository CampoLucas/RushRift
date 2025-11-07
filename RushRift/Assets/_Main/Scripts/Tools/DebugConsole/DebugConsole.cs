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
using UnityEngine.Serialization;
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
        private List<string> _usedInputs = new();
        private CursorLockMode _mode;
        private bool _isVisible;
        private int _index = -1;

        private static HelpCmd _help;
        private static DebugCommand _killAll;
        private static DebugCommand _unlimitedDashes; // Unlimited health, unlimited stamina
        private static DebugCommand<string> _setMedalString;
        private static DebugCommand<int> _setMedalInt;
        private static DebugCommand<int, string> _addMedalUpgrade;

        private List<object> _commandList;

        private void Awake()
        {
            _help = new HelpCmd();

            _commandList = new List<object>
            {
                _help,
                new DashHackCmd(),
                new KillAllCmd(),
                new SetMedalString(),
                new SetMedalInt(),
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
            _playerControls.Console.Close.performed -= OnClose;
            _playerControls.Console.Up.performed -= OnUp;
            _playerControls.Console.Down.performed -= OnDown;
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
            _index = -1;
            _help.ShowHelp = false;
            //_showHelp = false;
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
            _playerControls.Console.Up.performed += OnUp;
            _playerControls.Console.Down.performed += OnDown;
        }

        private void DisableDebug()
        {
            if (_inputManager)
            {
                _inputManager.enabled = true;
            }

            _input = "";

            CursorHandler.lockState = _mode;
            CursorHandler.visible = _isVisible;
            
            _playerControls.Console.Return.performed -= OnReturn;
            _playerControls.Console.Close.performed -= OnClose;
            _playerControls.Console.Up.performed -= OnUp;
            _playerControls.Console.Down.performed -= OnDown;
        }

        private void OnReturn(InputAction.CallbackContext obj)
        {
            HandleInput();
            this.Log($"[{_input}]");
            
            _usedInputs.Add(_input);
            _input = "";
        }
        
        private void OnClose(InputAction.CallbackContext obj)
        {
            _showConsole = false;
            DisableDebug();
            
        }
        
        private void OnUp(InputAction.CallbackContext obj)
        {
            if (_index <= -1)
            {
                _index = _usedInputs.Count - 1;
            }
            else
            {
                _index--;
            }

            _input = _index >= 0 ? _usedInputs[_index] : "";
        }
        
        private void OnDown(InputAction.CallbackContext obj)
        {
            if (_index >= _usedInputs.Count - 1)
            {
                _index = -1;
            }
            else
            {
                _index++;
            }
            
            _input = _index >= 0 ? _usedInputs[_index] : "";
        }

        private void OnGUI()
        {
            if (!_showConsole)
            {
                return;
            }

            var y = Screen.height - ConsoleHeight;
            var width = (float)Screen.width / 2;
            var height = 20f;
            var spacing = 5f;
            GUI.Box(new Rect(0, y, width, ConsoleHeight), "");
            
            y -= spacing;
            _input = GUI.TextField(new Rect(10f, y, width -= 20f, height), _input);

            if (_help.ShowHelp)
            {
                
                for (var i = 0; i < _commandList.Count; i++)
                {
                    y -= (1 + height);
                    var cmd = _commandList[i] as DebugCommandBase;
                    if (cmd == null) continue;
                    GUI.Label(new Rect(10f, y, width -= 20f, 20f), $"{cmd.Format} - {cmd.Description}");
                }
            }
            
            if (_usedInputs.Count > 0)
            {
                for (var i = _usedInputs.Count - 1; i >= 0; i--)
                {
                    y -= (1 + height);
                    GUI.Label(new Rect(10f, y, width -= 20f, 20f), _usedInputs[i]);
                }
                
            }
        }

        private void HandleInput()
        {
            var properties = _input.Split(' ');
            
            for (var i = 0; i < _commandList.Count; i++)
            {
                var args = properties.Length;
                
                if (_commandList[i] is not DebugCommandBase command || !_input.Contains(command.ID)) continue;

                if (properties.Length == 1 && command is DebugCommand c)
                {
                    c.Do();
                    return;
                }

                if (args <= 1) continue;
                var property1 = properties[1];
                if (int.TryParse(property1, out var parsedInt) && command is DebugCommand<int> cInt && cInt.Do(parsedInt))
                {
                    return;
                }

                if (command is DebugCommand<string> cString && cString.Do(property1))
                {
                    return;
                }
            }
        }
    }
}
