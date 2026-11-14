using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.LeaderboardDebugger
{
    /// <summary>
    /// Renders lists of checks with a Run button each and keeps the results shared by the Unit Tests sub-tabs.
    /// </summary>
    public sealed class CheckListView
    {
        private readonly Dictionary<string, CheckResult> results = new Dictionary<string, CheckResult>();

        public void RunAll(IEnumerable<Check> checks)
        {
            foreach (var result in LeaderboardChecks.ExecuteAll(checks)) results[result.Name] = result;
        }

        public void DrawSummary(List<Check> checks)
        {
            var names = checks.Select(check => check.Name).ToList();
            var known = results.Values.Where(result => names.Contains(result.Name)).ToList();
            EditorGUILayout.LabelField($"Passed: {known.Count(result => result.Passed)}   Failed: {known.Count(result => !result.Passed)}   Not run: {names.Count - known.Count}", EditorStyles.boldLabel);
        }

        public void Draw(string title, List<Check> checks)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                if (GUILayout.Button($"Run all {title.ToLower()}", GUILayout.Width(180))) RunAll(checks);
            }

            foreach (var check in checks)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Run", GUILayout.Width(40))) results[check.Name] = LeaderboardChecks.Execute(check);

                    EditorGUILayout.LabelField(check.Name);
                }

                if (results.TryGetValue(check.Name, out var shown))
                {
                    EditorGUILayout.HelpBox($"{(shown.Passed ? "PASS" : "FAIL")}: {shown.Message}", shown.Passed ? MessageType.Info : MessageType.Error);
                }
            }
        }
    }
}
