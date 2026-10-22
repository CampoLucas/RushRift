using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using PlayHookBridge = global::Tools.PlayHook.PlayLevelSelectionBridge;
using PlayHookToolbar = global::Tools.PlayHook.PlayLevelToolbar;

namespace Game.ConsoleCommands
{
    /// <summary>
    /// Lists the console command Edit Mode and Play Mode tests and runs them through Unity's TestRunnerApi.
    /// </summary>
    [Serializable]
    public sealed class ConsoleCommandsTestsView : ICallbacks
    {
        public enum TestState
        {
            NotRun,
            Running,
            Passed,
            Failed,
            Skipped
        }

        [Serializable]
        private sealed class TestEntry
        {
            public string fullName;
            public TestMode mode;
            public TestState state;
            public double duration;
            public string message;

            public string ShortName => fullName.Substring(fullName.LastIndexOf('.', fullName.Contains("(") ? fullName.IndexOf('(') : fullName.Length - 1) + 1);
        }

        private const string TestsNamespace = "Game.ConsoleCommands.Tests.";
        private const string PlayHookLevelKey = "PlayLevel.SelectedLevel";
        private const string PlayHookModeKey = "PlayLevel.SelectedMode";
        private const string SuspendedKey = "ConsoleCommands.PlayHookSuspended";
        private const string SavedLevelKey = "ConsoleCommands.PlayHookLevel";
        private const string SavedModeKey = "ConsoleCommands.PlayHookMode";

        [SerializeField] private List<TestEntry> entries = new List<TestEntry>();
        [SerializeField] private List<string> queuedPlayNames = new List<string>();
        [SerializeField] private bool isRunning;

        private TestRunnerApi api;
        private Action repaint;
        private Vector2 scroll;

        public void Enable(Action onRepaint)
        {
            repaint = onRepaint;
            api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(this);
            if (!isRunning) RestorePlayHook();
            Retrieve(TestMode.EditMode);
            Retrieve(TestMode.PlayMode);
        }

        public void Disable()
        {
            if (api == null) return;

            api.UnregisterCallbacks(this);
            UnityEngine.Object.DestroyImmediate(api);
            api = null;
        }

        public void Draw()
        {
            var inPlayMode = EditorApplication.isPlaying;
            if (inPlayMode) EditorGUILayout.HelpBox("Exit Play Mode to run Play Mode tests.", MessageType.Warning);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(isRunning || entries.Count == 0))
                {
                    if (GUILayout.Button("Run Edit Mode")) RunGroups(entries.Where(entry => entry.mode == TestMode.EditMode));

                    using (new EditorGUI.DisabledScope(inPlayMode))
                    {
                        if (GUILayout.Button("Run Play Mode")) RunGroups(entries.Where(entry => entry.mode == TestMode.PlayMode));
                        if (GUILayout.Button("Run Both")) RunGroups(entries);
                    }

                    if (GUILayout.Button("Run Failed")) RunGroups(entries.Where(entry => entry.state == TestState.Failed && (!inPlayMode || entry.mode == TestMode.EditMode)));
                }

                if (GUILayout.Button("Refresh List", GUILayout.Width(100))) RefreshList();
            }

            var passed = entries.Count(entry => entry.state == TestState.Passed);
            var failed = entries.Count(entry => entry.state == TestState.Failed);
            var notRun = entries.Count(entry => entry.state == TestState.NotRun);
            EditorGUILayout.LabelField($"Passed: {passed}   Failed: {failed}   Not run: {notRun}   Total: {entries.Count}", EditorStyles.boldLabel);

            if (isRunning) EditorGUILayout.HelpBox("Running tests... (Refresh List resets the state if a run was interrupted)", MessageType.Info);
            if (entries.Count == 0) EditorGUILayout.HelpBox("No console command tests found. Check that the test assemblies compiled, then press Refresh List.", MessageType.Warning);

            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(160));
            DrawGroup("Edit Mode", TestMode.EditMode, inPlayMode);
            EditorGUILayout.Space();
            DrawGroup("Play Mode", TestMode.PlayMode, inPlayMode);
            EditorGUILayout.EndScrollView();
        }

        public void RunStarted(ITestAdaptor testsToRun)
        {
            isRunning = true;
            repaint?.Invoke();
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            foreach (var entry in entries.Where(entry => entry.state == TestState.Running && !queuedPlayNames.Contains(entry.fullName))) entry.state = TestState.NotRun;

            if (queuedPlayNames.Count > 0 && !EditorApplication.isPlaying)
            {
                var names = queuedPlayNames.ToArray();
                queuedPlayNames.Clear();
                EditorApplication.delayCall += () => Execute(TestMode.PlayMode, names);
            }
            else
            {
                queuedPlayNames.Clear();
                isRunning = false;
                RestorePlayHook();
            }

            repaint?.Invoke();
        }

        public void TestStarted(ITestAdaptor test)
        {
        }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (result.Test.HasChildren) return;

            var entry = entries.FirstOrDefault(candidate => candidate.fullName == result.Test.FullName);
            if (entry == null) return;

            entry.state = ToState(result.TestStatus);
            entry.duration = result.Duration;
            entry.message = result.Message;
            repaint?.Invoke();
        }

        private void DrawGroup(string title, TestMode mode, bool inPlayMode)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            foreach (var entry in entries.Where(entry => entry.mode == mode))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(isRunning || (inPlayMode && mode == TestMode.PlayMode)))
                    {
                        if (GUILayout.Button("Run", GUILayout.Width(40))) RunGroups(new[] { entry });
                    }

                    var previous = GUI.contentColor;
                    GUI.contentColor = ColorFor(entry.state);
                    EditorGUILayout.LabelField(entry.state.ToString(), GUILayout.Width(60));
                    GUI.contentColor = previous;

                    EditorGUILayout.LabelField(new GUIContent(entry.ShortName, entry.fullName));
                    EditorGUILayout.LabelField(entry.state == TestState.NotRun || entry.state == TestState.Running ? "" : $"{entry.duration * 1000:0.0} ms", GUILayout.Width(70));
                }

                if ((entry.state == TestState.Failed || entry.state == TestState.Skipped) && !string.IsNullOrEmpty(entry.message))
                {
                    EditorGUILayout.HelpBox(entry.message.Trim(), entry.state == TestState.Failed ? MessageType.Error : MessageType.Info);
                }
            }
        }

        private void RefreshList()
        {
            isRunning = false;
            RestorePlayHook();
            queuedPlayNames.Clear();
            foreach (var entry in entries.Where(entry => entry.state == TestState.Running)) entry.state = TestState.NotRun;

            Retrieve(TestMode.EditMode);
            Retrieve(TestMode.PlayMode);
        }

        private void Retrieve(TestMode mode)
        {
            api.RetrieveTestList(mode, root =>
            {
                var previous = entries.Where(entry => entry.mode == mode).ToDictionary(entry => entry.fullName);
                var found = new List<TestEntry>();
                Collect(root, mode, previous, found);

                entries.RemoveAll(entry => entry.mode == mode);
                entries.AddRange(found);
                entries.Sort((a, b) => a.mode != b.mode ? a.mode.CompareTo(b.mode) : string.CompareOrdinal(a.fullName, b.fullName));
                repaint?.Invoke();
            });
        }

        private static void Collect(ITestAdaptor test, TestMode mode, Dictionary<string, TestEntry> previous, List<TestEntry> found)
        {
            if (!test.HasChildren)
            {
                if (!test.FullName.StartsWith(TestsNamespace)) return;

                found.Add(previous.TryGetValue(test.FullName, out var existing) ? existing : new TestEntry { fullName = test.FullName, mode = mode });
                return;
            }

            foreach (var child in test.Children) Collect(child, mode, previous, found);
        }

        private void RunGroups(IEnumerable<TestEntry> selection)
        {
            var selected = selection.ToList();
            var edit = selected.Where(entry => entry.mode == TestMode.EditMode).Select(entry => entry.fullName).ToArray();
            var play = selected.Where(entry => entry.mode == TestMode.PlayMode).Select(entry => entry.fullName).ToArray();
            if (edit.Length == 0 && play.Length == 0) return;

            foreach (var entry in selected)
            {
                entry.state = TestState.Running;
                entry.message = null;
            }

            if (edit.Length > 0)
            {
                queuedPlayNames.Clear();
                queuedPlayNames.AddRange(play);
                Execute(TestMode.EditMode, edit);
            }
            else
            {
                Execute(TestMode.PlayMode, play);
            }
        }

        private void Execute(TestMode mode, string[] names)
        {
            isRunning = true;
            if (mode == TestMode.PlayMode) SuspendPlayHook();
            api.Execute(new ExecutionSettings(new Filter { testMode = mode, testNames = names }));
        }

        private static void SuspendPlayHook()
        {
            if (!SessionState.GetBool(SuspendedKey, false))
            {
                SessionState.SetString(SavedLevelKey, EditorPrefs.GetString(PlayHookLevelKey, ""));
                SessionState.SetString(SavedModeKey, EditorPrefs.GetString(PlayHookModeKey, ""));
                SessionState.SetBool(SuspendedKey, true);
            }

            EditorPrefs.SetString(PlayHookLevelKey, PlayHookToolbar.DisabledFlag);
            EditorPrefs.SetString(PlayHookModeKey, PlayHookToolbar.DisabledFlag);
            EditorSceneManager.playModeStartScene = null;
        }

        private static void RestorePlayHook()
        {
            if (!SessionState.GetBool(SuspendedKey, false)) return;

            EditorPrefs.SetString(PlayHookLevelKey, SessionState.GetString(SavedLevelKey, ""));
            EditorPrefs.SetString(PlayHookModeKey, SessionState.GetString(SavedModeKey, ""));
            SessionState.EraseBool(SuspendedKey);
            PlayHookBridge.NotifyChanged();
        }

        private static TestState ToState(TestStatus status)
        {
            switch (status)
            {
                case TestStatus.Passed: return TestState.Passed;
                case TestStatus.Failed: return TestState.Failed;
                case TestStatus.Skipped:
                case TestStatus.Inconclusive: return TestState.Skipped;
                default: return TestState.NotRun;
            }
        }

        private static Color ColorFor(TestState state)
        {
            switch (state)
            {
                case TestState.Passed: return new Color(0.3f, 0.8f, 0.3f);
                case TestState.Failed: return new Color(0.9f, 0.3f, 0.3f);
                case TestState.Running: return new Color(0.9f, 0.8f, 0.3f);
                default: return GUI.contentColor;
            }
        }
    }
}
