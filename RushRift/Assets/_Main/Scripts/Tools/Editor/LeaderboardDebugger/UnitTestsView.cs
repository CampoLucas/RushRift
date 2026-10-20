using System;
using UnityEditor;
using UnityEngine;

namespace Game.LeaderboardDebugger
{
    public enum UnitSubTab
    {
        Checks,
        EdgeCases,
        Persistence,
        TestRunner
    }

    /// <summary>
    /// Unit Tests tab: a breadcrumb plus the Checks, Edge Cases, Persistence and Test Runner sub-tabs.
    /// </summary>
    [Serializable]
    public sealed class UnitTestsView
    {
        private const string SubTabKey = "LeaderboardDebugger.UnitSubTab";

        [SerializeField] private UnitSubTab subTab = UnitSubTab.Checks;

        private readonly CheckListView checkList = new CheckListView();
        private LeaderboardUnitTestsTab runner;

        public UnitSubTab SubTab => subTab;
        public bool UsesOwnScroll => subTab == UnitSubTab.TestRunner;

        public static string GetLabel(UnitSubTab sub)
        {
            return sub == UnitSubTab.EdgeCases ? "Edge Cases" : sub == UnitSubTab.TestRunner ? "Test Runner" : sub.ToString();
        }

        public void Enable(Action repaint)
        {
            subTab = (UnitSubTab)EditorPrefs.GetInt(SubTabKey, (int)UnitSubTab.Checks);
            runner = new LeaderboardUnitTestsTab(repaint);
            runner.Enable();
        }

        public void Disable()
        {
            runner?.Disable();
            runner = null;
        }

        public void Select(UnitSubTab sub)
        {
            subTab = sub;
            EditorPrefs.SetInt(SubTabKey, (int)sub);
        }

        public void Draw(DebuggerContext context)
        {
            EditorGUILayout.LabelField($"Unit Tests > {GetLabel(subTab)}", EditorStyles.boldLabel);

            switch (subTab)
            {
                case UnitSubTab.Checks: DrawChecks(context); break;
                case UnitSubTab.EdgeCases: DrawEdgeCases(context); break;
                case UnitSubTab.Persistence: DrawPersistence(context); break;
                case UnitSubTab.TestRunner: DrawTestRunner(context); break;
            }
        }

        private void DrawChecks(DebuggerContext context)
        {
            DebuggerHelp.Info(context, "Checks the leaderboard rules (sorting, ties, personal best) on generated data and on the saved data of the active level. Use it after changing the leaderboard code or to verify a save.");

            if (GUILayout.Button("Run all checks (every sub-tab)")) checkList.RunAll(LeaderboardChecks.Everything(context.Level));

            if (!context.HasValidLevel) DebuggerHelp.Warning($"There is no valid active level (id {context.Level}). The saved data check verifies the active level, so its result is not meaningful until you pick one.");

            checkList.DrawSummary(LeaderboardChecks.Everything(context.Level));
            EditorGUILayout.Space();
            checkList.Draw("Correctness", LeaderboardChecks.Correctness(context.Level));
        }

        private void DrawEdgeCases(DebuggerContext context)
        {
            DebuggerHelp.Info(context, "Tries extreme times (0, negative, huge, NaN), unusual names and ids, and empty or single-entry boards. Every check restores your real save when it finishes.");
            checkList.Draw("Edge cases", LeaderboardChecks.EdgeCases());
        }

        private void DrawPersistence(DebuggerContext context)
        {
            DebuggerHelp.Info(context, "Saves, reloads, deletes, corrupts and edits the save file to see how loading copes. Every check restores your real save when it finishes.");
            checkList.Draw("Persistence", LeaderboardChecks.Persistence());
        }

        private void DrawTestRunner(DebuggerContext context)
        {
            DebuggerHelp.Info(context, "Runs the NUnit Edit Mode tests through Unity's Test Runner API. The tests back up and restore your real save.");
            runner.Draw();
        }
    }
}
