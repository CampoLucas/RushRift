using System;
using UnityEditor;
using UnityEngine;

namespace Game.GhostDebugger
{
    public enum GhostUnitSubTab
    {
        Checks,
        ManualChecklist,
        TestRunner
    }

    /// <summary>
    /// Unit Tests tab: a breadcrumb plus the Checks, Manual Checklist and Test Runner sub-tabs.
    /// </summary>
    [Serializable]
    public sealed class GhostUnitTestsView
    {
        private const string SubTabKey = "GhostDebugger.UnitSubTab";

        [SerializeField] private GhostUnitSubTab subTab = GhostUnitSubTab.Checks;
        [SerializeField] private GhostManualChecklist checklist = new GhostManualChecklist();
        [SerializeField] private GhostTestRunnerTab runner = new GhostTestRunnerTab();

        private readonly GhostCheckListView checks = new GhostCheckListView();

        public GhostUnitSubTab SubTab => subTab;
        public bool UsesOwnScroll => subTab == GhostUnitSubTab.TestRunner;

        public static string GetLabel(GhostUnitSubTab sub)
        {
            return sub == GhostUnitSubTab.ManualChecklist ? "Manual Checklist" : sub == GhostUnitSubTab.TestRunner ? "Test Runner" : sub.ToString();
        }

        public void Enable(Action repaint)
        {
            subTab = (GhostUnitSubTab)EditorPrefs.GetInt(SubTabKey, (int)GhostUnitSubTab.Checks);
            runner.Enable(repaint);
        }

        public void Disable()
        {
            checks.Stop();
            runner.Disable();
        }

        public bool Update()
        {
            if (!checks.IsRunning) return false;

            checks.Update();
            return true;
        }

        public void Select(GhostUnitSubTab sub)
        {
            subTab = sub;
            EditorPrefs.SetInt(SubTabKey, (int)sub);
        }

        public void Draw(GhostDebuggerContext context)
        {
            EditorGUILayout.LabelField($"Unit Tests > {GetLabel(subTab)}", EditorStyles.boldLabel);

            switch (subTab)
            {
                case GhostUnitSubTab.Checks: checks.Draw(context); break;
                case GhostUnitSubTab.ManualChecklist: checklist.Draw(context); break;
                case GhostUnitSubTab.TestRunner: runner.Draw(context); break;
            }
        }
    }
}
