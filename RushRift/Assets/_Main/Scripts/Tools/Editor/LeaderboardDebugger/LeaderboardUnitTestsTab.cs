using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Game.LeaderboardDebugger
{
    public sealed class LeaderboardUnitTestsTab : ICallbacks
    {
        private enum TestState
        {
            NotRun,
            Running,
            Passed,
            Failed,
            Skipped
        }

        private sealed class TestEntry
        {
            public string FullName;
            public TestState State;
            public double Duration;
            public string Message;

            public string ShortName => FullName.Substring(FullName.LastIndexOf('.') + 1);
        }

        private const string FailedKey = "LeaderboardDebugger.FailedTests";
        private const string TestsNamespace = "Game.LeaderboardDebugger.Tests.";

        private readonly Action repaint;
        private readonly List<TestEntry> entries = new List<TestEntry>();
        private TestRunnerApi api;
        private Vector2 scroll;

        public bool IsRunning { get; private set; }

        public LeaderboardUnitTestsTab(Action repaint)
        {
            this.repaint = repaint;
        }

        public void Enable()
        {
            api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(this);
            RefreshList();
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
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(IsRunning || entries.Count == 0))
                {
                    if (GUILayout.Button("Run All")) Run(entries.Select(entry => entry.FullName));
                    if (GUILayout.Button("Run Failed")) Run(LoadFailedNames().Where(name => entries.Any(entry => entry.FullName == name)));
                }

                if (GUILayout.Button("Refresh List", GUILayout.Width(100))) RefreshList();
            }

            var passed = entries.Count(entry => entry.State == TestState.Passed);
            var failed = entries.Count(entry => entry.State == TestState.Failed);
            var notRun = entries.Count(entry => entry.State == TestState.NotRun);
            EditorGUILayout.LabelField($"Passed: {passed}   Failed: {failed}   Not run: {notRun}   Total: {entries.Count}", EditorStyles.boldLabel);

            if (IsRunning) EditorGUILayout.HelpBox("Running tests...", MessageType.Info);

            scroll = EditorGUILayout.BeginScrollView(scroll);

            foreach (var entry in entries)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(IsRunning))
                    {
                        if (GUILayout.Button("Run", GUILayout.Width(40))) Run(new[] { entry.FullName });
                    }

                    var previous = GUI.contentColor;
                    GUI.contentColor = ColorFor(entry.State);
                    EditorGUILayout.LabelField(entry.State.ToString(), GUILayout.Width(60));
                    GUI.contentColor = previous;

                    EditorGUILayout.LabelField(entry.ShortName);
                    EditorGUILayout.LabelField(entry.State == TestState.NotRun || entry.State == TestState.Running ? "" : $"{entry.Duration * 1000:0.0} ms", GUILayout.Width(70));
                }

                if (entry.State == TestState.Failed && !string.IsNullOrEmpty(entry.Message))
                {
                    EditorGUILayout.HelpBox(entry.Message.Trim(), MessageType.Error);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        public void RunStarted(ITestAdaptor testsToRun)
        {
            IsRunning = true;
            repaint();
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            IsRunning = false;
            foreach (var entry in entries.Where(entry => entry.State == TestState.Running)) entry.State = TestState.NotRun;
            SaveFailedNames();
            repaint();
        }

        public void TestStarted(ITestAdaptor test)
        {
        }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (result.Test.HasChildren) return;

            var entry = entries.FirstOrDefault(candidate => candidate.FullName == result.Test.FullName);
            if (entry == null) return;

            entry.State = ToState(result.TestStatus);
            entry.Duration = result.Duration;
            entry.Message = result.Message;
            repaint();
        }

        private void RefreshList()
        {
            api.RetrieveTestList(TestMode.EditMode, root =>
            {
                var previous = entries.ToDictionary(entry => entry.FullName);
                entries.Clear();
                Collect(root, previous);
                repaint();
            });
        }

        private void Collect(ITestAdaptor test, Dictionary<string, TestEntry> previous)
        {
            if (!test.HasChildren)
            {
                if (test.FullName.StartsWith(TestsNamespace))
                {
                    entries.Add(previous.TryGetValue(test.FullName, out var existing) ? existing : new TestEntry { FullName = test.FullName });
                }

                return;
            }

            foreach (var child in test.Children) Collect(child, previous);
        }

        private void Run(IEnumerable<string> names)
        {
            var selected = names.ToArray();
            if (selected.Length == 0) return;

            foreach (var entry in entries.Where(entry => selected.Contains(entry.FullName)))
            {
                entry.State = TestState.Running;
                entry.Message = null;
            }

            api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode, testNames = selected }));
        }

        private void SaveFailedNames()
        {
            var failed = entries.Where(entry => entry.State == TestState.Failed).Select(entry => entry.FullName);
            var remembered = LoadFailedNames().Where(name => entries.All(entry => entry.FullName != name || entry.State == TestState.NotRun));
            EditorPrefs.SetString(FailedKey, string.Join("\n", failed.Concat(remembered).Distinct()));
        }

        private static List<string> LoadFailedNames()
        {
            return EditorPrefs.GetString(FailedKey, "").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
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
