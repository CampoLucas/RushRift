using System;
using System.Collections.Generic;
using System.Linq;
using Game.Tools.DebugCommands;
using UnityEditor;
using UnityEngine;

namespace Game.ConsoleCommands
{
    /// <summary>
    /// Editor window that runs debug console commands, shows their output and runs the console command tests.
    /// In Play Mode it talks to the Debug Console in the scene; outside Play Mode it uses its own session and the console config asset.
    /// </summary>
    public sealed class ConsoleCommandsWindow : EditorWindow
    {
        [Serializable]
        private sealed class LogEntry
        {
            public string text;
            public bool success;
            public bool isInput;
        }

        private const string InputControl = "ConsoleCommandsInput";
        private const string PasswordControl = "ConsoleCommandsPassword";
        private const float LogHeight = 150f;
        private const int MaxLogEntries = 200;

        [SerializeField] private ConsoleCommandsTestsView tests = new ConsoleCommandsTestsView();
        [SerializeField] private List<LogEntry> log = new List<LogEntry>();
        [SerializeField] private string input = "";
        [SerializeField] private int modeIndex;

        private readonly IConsoleServices services = new GameConsoleServices();
        private ConsoleSession editorSession;
        private ConsoleConfigSO configAsset;
        private string password = "";
        private bool wasLoginInput;
        private bool focusInput;
        private GUIStyle titleStyle;
        private GUIStyle okStyle;
        private GUIStyle errorStyle;
        private Vector2 scroll;
        private Vector2 logScroll;

        [MenuItem("Tools/Console Commands")]
        public static void Open()
        {
            GetWindow<ConsoleCommandsWindow>("Console Commands");
        }

        private void OnEnable()
        {
            tests.Enable(Repaint);
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            tests.Disable();
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            Repaint();
        }

        private void OnInspectorUpdate()
        {
            if (EditorApplication.isPlaying) Repaint();
        }

        private void OnGUI()
        {
            DrawTitle();

            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawHeader();
            DrawCommandLine();
            DrawLog();
            DrawCommands();
            DrawTests();
            EditorGUILayout.EndScrollView();
        }

        private DebugConsole FindLiveConsole()
        {
            if (!EditorApplication.isPlaying) return null;

            var console = FindObjectOfType<DebugConsole>();
            return console && console.Commands != null ? console : null;
        }

        private ConsoleConfigSO FindConfig()
        {
            if (configAsset) return configAsset;

            var guid = AssetDatabase.FindAssets($"t:{nameof(ConsoleConfigSO)}").FirstOrDefault();
            if (guid != null) configAsset = AssetDatabase.LoadAssetAtPath<ConsoleConfigSO>(AssetDatabase.GUIDToAssetPath(guid));
            return configAsset;
        }

        private ConsoleCommandSet GetCommands(DebugConsole live)
        {
            if (live) return live.Commands;

            editorSession ??= new ConsoleSession(null);
            return ConsoleCommandSet.Create(FindConfig(), editorSession, services);
        }

        private void DrawTitle()
        {
            titleStyle ??= new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 14 };
            EditorGUILayout.LabelField("CONSOLE COMMANDS", titleStyle);
        }

        private void DrawHeader()
        {
            var live = FindLiveConsole();
            var commands = GetCommands(live);
            var config = live ? live.Config : FindConfig();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var previous = GUI.contentColor;
                    GUI.contentColor = commands.Session.LoggedIn ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.9f, 0.3f, 0.3f);
                    EditorGUILayout.LabelField(commands.Session.LoggedIn ? "Logged in" : "Logged out", EditorStyles.boldLabel, GUILayout.Width(90));
                    GUI.contentColor = previous;

                    EditorGUILayout.LabelField(EditorApplication.isPlaying ? "Play Mode" : "Edit Mode", GUILayout.Width(80));
                    EditorGUILayout.LabelField(live ? "Source: Debug Console in the scene" : "Source: console config asset", EditorStyles.miniLabel);
                }

                if (EditorApplication.isPlaying && !live)
                {
                    EditorGUILayout.HelpBox("No Debug Console is running in the scene. Commands use the window's own session, so old console commands can't run.", MessageType.Warning);
                }

                if (!config)
                {
                    EditorGUILayout.HelpBox(live ? "The Debug Console has no Console Config assigned. Login will always fail." : "No Console Config asset was found. Create one with Create > Game > Debug Console Config.", MessageType.Error);
                }
                else
                {
                    EditorGUILayout.ObjectField("Config", config, typeof(ConsoleConfigSO), false);
                    if (string.IsNullOrEmpty(config.Password)) EditorGUILayout.HelpBox("The console config has no password. Login will always fail.", MessageType.Warning);
                }
            }
        }

        private void DrawCommandLine()
        {
            var current = Event.current;
            var focused = GUI.GetNameOfFocusedControl();
            var submit = current.type == EventType.KeyDown
                && (current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter)
                && (focused == InputControl || focused == PasswordControl);

            if (submit) current.Use();

            EditorGUILayout.LabelField("Command", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                input = DrawInput(input);

                if (GUILayout.Button("Execute", GUILayout.Width(70)) || submit)
                {
                    Run(input);
                    input = "";
                    GUIUtility.keyboardControl = 0;
                    focusInput = true;
                }
            }
        }

        private string DrawInput(string text)
        {
            var prefixLength = ConsoleCommandSet.LoginPrefixLength(text);
            var isLogin = prefixLength > 0;
            var switched = isLogin != wasLoginInput;
            wasLoginInput = isLogin;

            if (!isLogin)
            {
                GUI.SetNextControlName(InputControl);
                var typed = EditorGUILayout.TextField(text ?? "");

                if (switched || focusInput)
                {
                    EditorGUI.FocusTextInControl(InputControl);
                    focusInput = false;
                }

                return typed;
            }

            var prefix = text.Substring(0, prefixLength);
            var hidden = text.Substring(prefixLength);
            var current = Event.current;

            if (hidden.Length == 0 && current.type == EventType.KeyDown && current.keyCode == KeyCode.Backspace && GUI.GetNameOfFocusedControl() == PasswordControl)
            {
                current.Use();
                return prefix.Substring(0, prefix.Length - 1);
            }

            GUILayout.Label(prefix, GUILayout.ExpandWidth(false));
            GUI.SetNextControlName(PasswordControl);
            hidden = EditorGUILayout.PasswordField(hidden);

            if (switched) EditorGUI.FocusTextInControl(PasswordControl);

            return prefix + hidden;
        }

        private void DrawLog()
        {
            okStyle ??= new GUIStyle(EditorStyles.label) { wordWrap = true, richText = false };
            errorStyle ??= new GUIStyle(okStyle) { normal = { textColor = new Color(0.9f, 0.35f, 0.35f) } };

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
                if (GUILayout.Button("Clear", GUILayout.Width(60))) log.Clear();
            }

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                logScroll = EditorGUILayout.BeginScrollView(logScroll, GUILayout.Height(LogHeight));

                if (log.Count == 0) EditorGUILayout.LabelField("No output yet.", EditorStyles.miniLabel);

                foreach (var entry in log)
                {
                    if (entry.isInput) EditorGUILayout.LabelField($"> {entry.text}", EditorStyles.boldLabel);
                    else EditorGUILayout.LabelField(entry.text, entry.success ? okStyle : errorStyle);
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawCommands()
        {
            var live = FindLiveConsole();
            var commands = GetCommands(live);
            var playing = EditorApplication.isPlaying;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Commands", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Password", GUILayout.Width(70));
                    password = EditorGUILayout.PasswordField(password);

                    if (GUILayout.Button("Login", GUILayout.Width(70)))
                    {
                        Run($"login {password}");
                        password = "";
                    }

                    if (GUILayout.Button("Logout", GUILayout.Width(70))) Run("logout");
                    if (GUILayout.Button("clear", GUILayout.Width(70))) Run("clear");
                }

                EditorGUILayout.Space();

                if (!playing) EditorGUILayout.HelpBox("load-gm, disable-ghost and enable-ghost need Play Mode. Enter Play Mode to use them.", MessageType.Info);

                using (new EditorGUI.DisabledScope(!playing))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var ids = commands.GameModeIds.ToArray();
                        EditorGUILayout.LabelField("Game mode", GUILayout.Width(70));

                        if (ids.Length == 0)
                        {
                            EditorGUILayout.LabelField("No game modes in the console config", EditorStyles.miniLabel);
                        }
                        else
                        {
                            modeIndex = Mathf.Clamp(modeIndex, 0, ids.Length - 1);
                            modeIndex = EditorGUILayout.Popup(modeIndex, ids);
                            if (GUILayout.Button("load-gm", GUILayout.Width(100))) Run($"load-gm {ids[modeIndex]}");
                        }
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Ghost", GUILayout.Width(70));
                        if (GUILayout.Button("disable-ghost")) Run("disable-ghost");
                        if (GUILayout.Button("enable-ghost")) Run("enable-ghost");
                    }
                }

                EditorGUILayout.Space();

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Data", GUILayout.Width(70));
                    if (GUILayout.Button("clear-settings-data")) Confirm("Clear settings data", "Reset the settings save to its defaults?", "clear-settings-data");
                    if (GUILayout.Button("clear-save-data")) Confirm("Clear save data", "Reset the save and progress data to its defaults? This also deletes every saved ghost recording.", "clear-save-data");
                    if (GUILayout.Button("clear-leaderboard-data")) Confirm("Clear leaderboard data", "Remove every leaderboard entry of every level?", "clear-leaderboard-data");
                }

                EditorGUILayout.LabelField(playing ? "Clear commands change the data of the running game." : "Clear commands change the editor save files on this machine.", EditorStyles.miniLabel);
            }
        }

        private void Confirm(string title, string message, string command)
        {
            if (EditorUtility.DisplayDialog(title, $"{message}\n\nThis can't be undone.", "Clear", "Cancel")) Run(command);
        }

        private void DrawTests()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Run tests", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                tests.Draw();
            }
        }

        private void Run(string text)
        {
            var live = FindLiveConsole();
            var commands = GetCommands(live);
            var cleared = false;
            void OnHistoryCleared() => cleared = true;
            ConsoleResult result;

            commands.Session.HistoryCleared += OnHistoryCleared;

            try
            {
                if (live) result = live.Execute(text);
                else if (!commands.TryExecute(text, out result)) result = ConsoleResult.Error("Old console commands only run in Play Mode with the Debug Console in the scene.");
            }
            finally
            {
                commands.Session.HistoryCleared -= OnHistoryCleared;
            }

            if (cleared) log.Clear();

            Add(new LogEntry { text = ConsoleCommandSet.MaskInput(text), isInput = true });
            Add(new LogEntry { text = result.Message, success = result.Success });
            logScroll.y = float.MaxValue;
            Repaint();
        }

        private void Add(LogEntry entry)
        {
            log.Add(entry);
            if (log.Count > MaxLogEntries) log.RemoveRange(0, log.Count - MaxLogEntries);
        }
    }
}
