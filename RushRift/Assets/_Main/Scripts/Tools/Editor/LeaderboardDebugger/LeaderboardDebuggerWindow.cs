using System;
using UnityEditor;
using UnityEngine;

namespace Game.LeaderboardDebugger
{
    /// <summary>
    /// Editor window with a level header and the Generate, Data, Entries and Unit Tests tabs.
    /// </summary>
    public sealed class LeaderboardDebuggerWindow : EditorWindow
    {
        private enum Tab
        {
            Generate,
            Data,
            Entries,
            UnitTests
        }

        private const float TabHeight = 24f;
        private const float HelpToggleWidth = 90f;

        [SerializeField] private Tab tab;
        [SerializeField] private DebuggerContext context = new DebuggerContext();
        [SerializeField] private GenerateTab generate = new GenerateTab();
        [SerializeField] private DataTab data = new DataTab();
        [SerializeField] private UnitTestsView unitTests = new UnitTestsView();

        private readonly EntriesTab entries = new EntriesTab();
        private GUIStyle titleStyle;
        private Vector2 scroll;
        private double lastPoll;

        [MenuItem("Tools/Leaderboard Debugger")]
        public static void Open()
        {
            GetWindow<LeaderboardDebuggerWindow>("Leaderboard Debugger");
        }

        private void OnEnable()
        {
            context.LoadPreferences();
            unitTests.Enable(Repaint);
            LeaderboardLevelSource.Reload();
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            unitTests.Disable();
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void OnFocus()
        {
            context.Invalidate();
        }

        private void OnEditorUpdate()
        {
            if (EditorApplication.timeSinceStartup - lastPoll < 0.5) return;

            lastPoll = EditorApplication.timeSinceStartup;
            if (context.Poll()) Repaint();
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            context.Invalidate();
            Repaint();
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.Layout) context.Poll();

            DrawTitle();
            DrawHeader();
            DrawTabBar();
            DrawContent();
        }

        private void DrawTitle()
        {
            titleStyle ??= new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 14 };

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(HelpToggleWidth);
                EditorGUILayout.LabelField("LEADERBOARD DEBUGGER", titleStyle, GUILayout.ExpandWidth(true));
                context.ShowHelp = GUILayout.Toggle(context.ShowHelp, "Show help", GUILayout.Width(HelpToggleWidth));
            }
        }

        private void DrawHeader()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Level", GUILayout.Width(40));

                    var content = new GUIContent(LeaderboardLevelSource.GetLabel(context.Mode, context.Level));
                    var rect = GUILayoutUtility.GetRect(content, EditorStyles.popup, GUILayout.ExpandWidth(true));
                    if (EditorGUI.DropdownButton(rect, content, FocusType.Passive))
                    {
                        LeaderboardLevelSource.ShowDropdown(rect, context.Mode, context.Level, (mode, id) =>
                        {
                            context.SetSelection(mode, id);
                            Repaint();
                        });
                    }
                }

                DebuggerHelp.Info(context, "Chooses which level every tab works on. Current follows the level running in Play Mode, or the level picked in the PlayHook toolbar while the game is stopped.");

                if (context.Mode == LevelMode.Current && !context.LevelResolved)
                {
                    DebuggerHelp.Warning($"There is no current level: nothing is running and PlayHook has no level selected. Level id {context.Level} is used instead.");
                }

                if (context.Mode == LevelMode.Custom) DrawCustomId();

                EditorGUILayout.LabelField($"Active level id {context.Level} ({context.LevelSource})", EditorStyles.miniLabel);
            }
        }

        private void DrawCustomId()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Id", GUILayout.Width(40));

                var edited = EditorGUILayout.IntField(context.ManualLevel);
                if (edited != context.ManualLevel) context.SetManualLevel(edited);
            }

            DebuggerHelp.Info(context, "Leaderboard data is stored per level id. Type any id, including one that has no level asset.");

            if (context.ManualLevel < 0) DebuggerHelp.Warning("Level ids are not negative in the game, and -1 means there is no level.");
            else if (!LeaderboardLevelSource.IsKnown(context.ManualLevel)) DebuggerHelp.Warning($"No level asset in the project has the id {context.ManualLevel}, so the game will never show this board.");
        }

        private void DrawTabBar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawTab(Tab.Generate, "Generate", EditorStyles.miniButtonLeft);
                DrawTab(Tab.Data, "Data", EditorStyles.miniButtonMid);
                DrawTab(Tab.Entries, "Entries", EditorStyles.miniButtonMid);
                SplitTabButton.Draw("Unit Tests", tab == Tab.UnitTests, TabHeight, () => tab = Tab.UnitTests, OpenUnitTestsMenu);
            }
        }

        private void DrawTab(Tab target, string label, GUIStyle style)
        {
            if (GUILayout.Toggle(tab == target, label, style, GUILayout.Height(TabHeight)) && tab != target) tab = target;
        }

        private void OpenUnitTestsMenu(Rect rect)
        {
            var menu = new GenericMenu();

            foreach (UnitSubTab sub in Enum.GetValues(typeof(UnitSubTab)))
            {
                var captured = sub;
                menu.AddItem(new GUIContent(UnitTestsView.GetLabel(sub)), tab == Tab.UnitTests && unitTests.SubTab == sub, () =>
                {
                    tab = Tab.UnitTests;
                    unitTests.Select(captured);
                    Repaint();
                });
            }

            menu.DropDown(rect);
        }

        private void DrawContent()
        {
            var ownScroll = tab == Tab.UnitTests && unitTests.UsesOwnScroll;

            using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.ExpandHeight(true)))
            {
                if (!ownScroll) scroll = EditorGUILayout.BeginScrollView(scroll);

                switch (tab)
                {
                    case Tab.Generate: generate.Draw(context); break;
                    case Tab.Data: data.Draw(context); break;
                    case Tab.Entries: entries.Draw(context); break;
                    case Tab.UnitTests: unitTests.Draw(context); break;
                }

                if (!ownScroll) EditorGUILayout.EndScrollView();
            }
        }
    }
}
