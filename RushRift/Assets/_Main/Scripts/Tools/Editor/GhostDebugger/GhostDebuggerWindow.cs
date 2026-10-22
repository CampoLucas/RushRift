using System;
using Game.LeaderboardDebugger;
using UnityEditor;
using UnityEngine;

namespace Game.GhostDebugger
{
    /// <summary>
    /// Editor window with a ghost header and the Playback, Data, Scene, Files and Unit Tests tabs.
    /// </summary>
    public sealed class GhostDebuggerWindow : EditorWindow
    {
        private enum Tab
        {
            Playback,
            Data,
            Scene,
            Files,
            UnitTests
        }

        private const float TabHeight = 24f;
        private const float HelpToggleWidth = 90f;

        [SerializeField] private Tab tab;
        [SerializeField] private GhostDebuggerContext context = new GhostDebuggerContext();
        [SerializeField] private GhostPlaybackTab playback = new GhostPlaybackTab();
        [SerializeField] private GhostDataTab data = new GhostDataTab();
        [SerializeField] private GhostSceneTab scene = new GhostSceneTab();
        [SerializeField] private GhostFilesTab files = new GhostFilesTab();
        [SerializeField] private GhostUnitTestsView unitTests = new GhostUnitTestsView();

        private GUIStyle titleStyle;
        private Vector2 scroll;
        private double lastPoll;

        [MenuItem("Tools/Ghost Debugger")]
        public static void Open()
        {
            GetWindow<GhostDebuggerWindow>("Ghost Debugger");
        }

        private void OnEnable()
        {
            context.LoadPreferences();
            context.WindowOpened();
            context.MarkSceneDirty();
            unitTests.Enable(Repaint);
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.hierarchyChanged += context.MarkSceneDirty;
            SceneView.duringSceneGui += OnSceneGui;
        }

        private void OnDisable()
        {
            unitTests.Disable();
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.hierarchyChanged -= context.MarkSceneDirty;
            SceneView.duringSceneGui -= OnSceneGui;
        }

        private void OnDestroy()
        {
            context.WindowClosed();
        }

        private void OnEditorUpdate()
        {
            var changed = context.Tick();
            changed |= unitTests.Update();

            if (EditorApplication.timeSinceStartup - lastPoll >= 0.5 || changed)
            {
                lastPoll = EditorApplication.timeSinceStartup;
                changed |= context.Poll();
            }

            if (!changed) return;

            Repaint();
            SceneView.RepaintAll();
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            context.OnPlayModeChanged();
            Repaint();
        }

        private void OnSceneGui(SceneView view)
        {
            if (Event.current.type == EventType.Repaint) GhostSceneDrawer.Draw(context);
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
                EditorGUILayout.LabelField("GHOST DEBUGGER", titleStyle, GUILayout.ExpandWidth(true));
                context.ShowHelp = GUILayout.Toggle(context.ShowHelp, "Show help", GUILayout.Width(HelpToggleWidth));
            }
        }

        private void DrawHeader()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Ghost", GUILayout.Width(40));

                    var content = new GUIContent(context.Source == GhostSource.Scene ? "Scene ghost (live)" : $"File: {context.RunName}");
                    var rect = GUILayoutUtility.GetRect(content, EditorStyles.popup, GUILayout.ExpandWidth(true));
                    if (EditorGUI.DropdownButton(rect, content, FocusType.Passive)) ShowGhostMenu(rect);
                }

                GhostDebuggerHelp.Info(context, "Chooses the ghost every tab works on. Scene ghost follows the GhostPlayer running in Play Mode. A file opens a saved ghost as a preview, also outside Play Mode.");

                if (context.Source == GhostSource.Scene && !context.Player)
                {
                    GhostDebuggerHelp.Warning(Application.isPlaying ? "No GhostPlayer in the open scenes." : "Enter Play Mode in a level to use the scene ghost, or pick a file.");
                }
                else if (context.RunError != null)
                {
                    GhostDebuggerHelp.Error($"The file could not be read: {context.RunError}");
                }
                else if (context.Source == GhostSource.Scene && !context.HasRun)
                {
                    GhostDebuggerHelp.Warning("The scene ghost has no run loaded (no best ghost for this level yet, or the file was rejected).");
                }

                if (context.HasRun)
                {
                    var validity = context.RunIssues.Count == 0 ? "valid" : "INVALID";
                    EditorGUILayout.LabelField($"{context.RunName} · {validity} · {context.Frames.Count} samples · {context.Run.durationSeconds:0.000} s · format v{context.Run.formatVersion}", EditorStyles.miniLabel);
                }

                if (context.UseSandbox) EditorGUILayout.LabelField("Game ghost saves go to the sandbox folder (Files tab).", EditorStyles.miniLabel);
            }
        }

        private void ShowGhostMenu(Rect rect)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Scene ghost (live)"), context.Source == GhostSource.Scene, () =>
            {
                context.SelectScene();
                Repaint();
            });
            menu.AddSeparator("");

            AddFiles(menu, "Sandbox", GhostDebuggerService.SandboxFolder);
            AddFiles(menu, "Real save (read only)", GhostDebuggerService.RealFolder);

            menu.AddItem(new GUIContent("Open file..."), false, () =>
            {
                var path = EditorUtility.OpenFilePanel("Open ghost", context.ExportFolder, "json");
                if (string.IsNullOrEmpty(path)) return;

                context.SelectFile(path);
                Repaint();
            });

            menu.DropDown(rect);
        }

        private void AddFiles(GenericMenu menu, string group, string folder)
        {
            var names = GhostDebuggerService.ListGhosts(folder);
            if (names.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent($"{group}/(empty)"));
                return;
            }

            foreach (var name in names)
            {
                var path = GhostDebuggerService.PathFor(folder, name);
                menu.AddItem(new GUIContent($"{group}/{name}"), context.Source == GhostSource.File && context.FilePath == path, () =>
                {
                    context.SelectFile(path);
                    Repaint();
                });
            }
        }

        private void DrawTabBar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawTab(Tab.Playback, "Playback", EditorStyles.miniButtonLeft);
                DrawTab(Tab.Data, "Data", EditorStyles.miniButtonMid);
                DrawTab(Tab.Scene, "Scene", EditorStyles.miniButtonMid);
                DrawTab(Tab.Files, "Files", EditorStyles.miniButtonMid);
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

            foreach (GhostUnitSubTab sub in Enum.GetValues(typeof(GhostUnitSubTab)))
            {
                var captured = sub;
                menu.AddItem(new GUIContent(GhostUnitTestsView.GetLabel(sub)), tab == Tab.UnitTests && unitTests.SubTab == sub, () =>
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
                    case Tab.Playback: playback.Draw(context); break;
                    case Tab.Data: data.Draw(context); break;
                    case Tab.Scene: scene.Draw(context); break;
                    case Tab.Files: files.Draw(context); break;
                    case Tab.UnitTests: unitTests.Draw(context); break;
                }

                if (!ownScroll) EditorGUILayout.EndScrollView();
            }
        }
    }
}
