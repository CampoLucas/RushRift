using System;
using System.Collections;
using System.Collections.Generic;
#if LEADERBOARD_CMD_ENABLED
using System.Linq;
#endif
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
        private const string InputControl = "DebugConsoleInput";
        private const string PasswordControl = "DebugConsolePassword";

        [SerializeField] private ConsoleConfigSO config;

        public ConsoleConfigSO Config => config;
        public ConsoleCommandSet Commands { get; private set; }

        private InputManager _inputManager;
        private PlayerControls _playerControls;
        private bool _showConsole;
        private string _input;
        private List<string> _usedInputs = new();
        private CursorLockMode _mode;
        private bool _isVisible;
        private int _index = -1;
        private string _lastOutput;
        private bool _focusInput;

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

            Commands = ConsoleCommandSet.Create(config, ConsoleSession.Shared, new GameConsoleServices());

            _commandList = new List<object> { _help };
            _commandList.AddRange(Commands.Commands);

#if UNITY_EDITOR || CHEATS_ENABLED
            _commandList.AddRange(new object[]
            {
                new DashHackCmd(),
                new KillAllCmd(),
                new SetMedalString(),
                new SetMedalInt(),
                new ToggleHUDCmd(),
                new TogglePauseUICmd()
            });
#endif

#if LEADERBOARD_CMD_ENABLED
            _commandList.AddRange(LeaderboardCmdUtils.CreateCommands());
#endif

            _inputManager = GetComponent<InputManager>();
            Commands.Session.HistoryCleared += ClearHistory;
        }

        private void OnDestroy()
        {
            if (Commands != null) Commands.Session.HistoryCleared -= ClearHistory;
        }

        private void ClearHistory()
        {
            _usedInputs.Clear();
            _index = -1;
            _lastOutput = null;
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
            var shown = ConsoleCommandSet.MaskInput(_input);
            this.Log($"[{shown}]");
            _usedInputs.Add(shown);
            _lastOutput = Execute(_input).Message;
            _input = "";
            _focusInput = true;
        }

        public ConsoleResult Execute(string input)
        {
            if (Commands.TryExecute(input, out var result)) return result;

            result = HandleInput(input) ? ConsoleResult.Ok($"Ran: {input}") : ConsoleResult.Error($"Unknown command or wrong arguments: {input}");
            ConsoleCommandSet.Print(result);
            return result;
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
            _input = DrawInput(new Rect(10f, y, width -= 20f, height), _input);

            if (!string.IsNullOrEmpty(_lastOutput))
            {
                y -= (1 + height);
                GUI.Label(new Rect(10f, y, width, height), _lastOutput);
            }

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

        private string DrawInput(Rect rect, string input)
        {
            var prefixLength = ConsoleCommandSet.LoginPrefixLength(input);

            if (prefixLength == 0)
            {
                GUI.SetNextControlName(InputControl);
                var typed = GUI.TextField(rect, input ?? "");

                if (_focusInput)
                {
                    GUI.FocusControl(InputControl);
                    _focusInput = false;
                }

                return typed;
            }

            var prefix = input.Substring(0, prefixLength);
            var password = input.Substring(prefixLength);
            var prefixWidth = GUI.skin.label.CalcSize(new GUIContent(prefix)).x;
            var current = Event.current;

            if (password.Length == 0 && current.type == EventType.KeyDown && current.keyCode == KeyCode.Backspace && GUI.GetNameOfFocusedControl() == PasswordControl)
            {
                current.Use();
                _focusInput = true;
                return prefix.Substring(0, prefix.Length - 1);
            }

            GUI.Label(new Rect(rect.x, rect.y, prefixWidth, rect.height), prefix);
            GUI.SetNextControlName(PasswordControl);
            password = GUI.PasswordField(new Rect(rect.x + prefixWidth, rect.y, rect.width - prefixWidth, rect.height), password, ConsoleCommandSet.MaskCharacter);

            if (GUI.GetNameOfFocusedControl() != PasswordControl) GUI.FocusControl(PasswordControl);

            return prefix + password;
        }

        private bool HandleInput(string input)
        {
            var properties = input.Split(' ');
            
            for (var i = 0; i < _commandList.Count; i++)
            {
                var args = properties.Length;
                
                if (_commandList[i] is not DebugCommandBase command || !input.Contains(command.ID)) continue;

                if (properties.Length == 1 && command is DebugCommand c)
                {
                    c.Do();
                    return true;
                }

#if LEADERBOARD_CMD_ENABLED
                if (command is DebugCommandArgs cArgs)
                {
                    cArgs.Do(properties.Skip(1).Where(word => word.Length > 0).ToArray());
                    return true;
                }
#endif

                if (args <= 1) continue;
                var property1 = properties[1];
                if (int.TryParse(property1, out var parsedInt) && command is DebugCommand<int> cInt && cInt.Do(parsedInt))
                {
                    return true;
                }

                if (command is DebugCommand<string> cString && cString.Do(property1))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
