using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.LeaderboardDebugger
{
    public sealed class LeaderboardDebuggerWindow : EditorWindow
    {
        private static readonly string[] TabNames = { "Generate", "Inspect", "Checks", "Edge Cases", "Persistence", "Data", "Unit Tests" };

        [SerializeField] private int tab;
        [SerializeField] private int manualLevel = 1;
        [SerializeField] private bool useCurrentLevel = true;
        [SerializeField] private bool showLiveEntries = true;
        [SerializeField] private int generateCount = 50;
        [SerializeField] private ScoreDistribution distribution = ScoreDistribution.Uniform;
        [SerializeField] private float minTime = 20f;
        [SerializeField] private float maxTime = 300f;
        [SerializeField] private string singleName = "TestPlayer";
        [SerializeField] private string singleId = "";
        [SerializeField] private float singleTime = 60f;
        [SerializeField] private int tieCount = 5;
        [SerializeField] private float tieTime = 60f;

        private readonly Dictionary<string, CheckResult> results = new Dictionary<string, CheckResult>();
        private LeaderboardUnitTestsTab unitTestsTab;
        private Vector2 scroll;
        private Vector2 liveScroll;
        private double lastPoll;
        private long liveStamp = -1;
        private int liveLevel = int.MinValue;
        private string liveError;
        private string levelSource = "";
        private List<Score> liveEntries = new List<Score>();
        private bool inspectDirty = true;
        private bool showHex;
        private string inspectError;
        private string inspectDescription;
        private string inspectHex;
        private List<Score> inspectEntries = new List<Score>();
        private List<int> inspectLevels = new List<int>();

        private int level { get; set; } = 1;


        [MenuItem("Tools/Leaderboard Debugger")]
        public static void Open()
        {
            GetWindow<LeaderboardDebuggerWindow>("Leaderboard Debugger");
        }

        private void OnEnable()
        {
            unitTestsTab = new LeaderboardUnitTestsTab(Repaint);
            unitTestsTab.Enable();
            LeaderboardLevelSource.Reload();
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            unitTestsTab?.Disable();
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void OnEditorUpdate()
        {
            if (EditorApplication.timeSinceStartup - lastPoll < 0.5) return;

            lastPoll = EditorApplication.timeSinceStartup;
            if (PollLive()) Repaint();
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            liveStamp = -1;
            Repaint();
        }

        private bool PollLive()
        {
            LeaderboardLevelSource.TryResolve(useCurrentLevel, manualLevel, out var resolved, out levelSource);
            level = resolved;

            var path = LeaderboardDebuggerService.SavePath;
            var info = new FileInfo(path);
            var stamp = info.Exists ? info.LastWriteTimeUtc.Ticks ^ info.Length : 0L;

            if (stamp == liveStamp && level == liveLevel) return false;

            liveStamp = stamp;
            liveLevel = level;
            inspectDirty = true;
            liveEntries = new List<Score>();
            liveError = null;

            if (!LeaderboardDebuggerService.TryReadWithoutRepair(out var data, out liveError)) return true;

            liveEntries = LeaderboardDebuggerService.GetEntries(data, level);
            return true;
        }

        private void OnFocus()
        {
            inspectDirty = true;
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.Layout) PollLive();
            DrawLevelSection();
            DrawLiveEntries();
            var selected = GUILayout.Toolbar(tab, TabNames);
            if (selected != tab)
            {
                tab = selected;
                inspectDirty = true;
            }

            EditorGUILayout.Space();

            if (tab == 6)
            {
                unitTestsTab.Draw();
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);

            switch (tab)
            {
                case 0: DrawGenerate(); break;
                case 1: DrawInspect(); break;
                case 2: DrawChecks(); break;
                case 3: DrawCheckList("Edge cases", LeaderboardChecks.EdgeCases()); break;
                case 4: DrawPersistence(); break;
                case 5: DrawData(); break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawLevelSection()
        {
            useCurrentLevel = EditorGUILayout.ToggleLeft("Use current level (Play Mode level, otherwise PlayHook selection)", useCurrentLevel);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Level", GUILayout.Width(40));

                var content = new GUIContent(LeaderboardLevelSource.GetLabel(level));
                var rect = GUILayoutUtility.GetRect(content, EditorStyles.popup, GUILayout.MinWidth(120));
                if (EditorGUI.DropdownButton(rect, content, FocusType.Passive))
                {
                    LeaderboardLevelSource.ShowDropdown(rect, level, id =>
                    {
                        manualLevel = id;
                        useCurrentLevel = false;
                        Repaint();
                    });
                }

                var edited = EditorGUILayout.IntField(level, GUILayout.Width(60));
                if (edited != level)
                {
                    manualLevel = edited;
                    useCurrentLevel = false;
                }
            }

            EditorGUILayout.LabelField($"Active level id {level} ({levelSource})", EditorStyles.miniLabel);
        }

        private void DrawLiveEntries()
        {
            showLiveEntries = EditorGUILayout.Foldout(showLiveEntries, $"Current entries, level {level} ({liveEntries.Count})", true);

            if (!showLiveEntries) return;

            if (liveError != null)
            {
                EditorGUILayout.HelpBox(liveError, MessageType.Warning);
                return;
            }

            if (liveEntries.Count == 0)
            {
                EditorGUILayout.HelpBox($"Level {level} has no entries.", MessageType.Info);
                return;
            }

            liveScroll = EditorGUILayout.BeginScrollView(liveScroll, GUILayout.MaxHeight(150));

            for (var i = 0; i < liveEntries.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"#{i + 1}", GUILayout.Width(40));
                    EditorGUILayout.LabelField(liveEntries[i].name, GUILayout.MinWidth(80));
                    EditorGUILayout.LabelField(LeaderboardDebuggerService.FormatTime(liveEntries[i].timescore), GUILayout.Width(100));
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawGenerate()
        {
            EditorGUILayout.LabelField("Random entries", EditorStyles.boldLabel);
            generateCount = Mathf.Max(0, EditorGUILayout.IntField("Count", generateCount));
            distribution = (ScoreDistribution)EditorGUILayout.EnumPopup("Distribution", distribution);
            minTime = EditorGUILayout.FloatField("Min time", minTime);
            maxTime = EditorGUILayout.FloatField("Max time", maxTime);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add to level"))
                {
                    LeaderboardDebuggerService.AddEntries(level, LeaderboardGenerator.Generate(generateCount, distribution, minTime, Mathf.Max(minTime, maxTime)));
                    inspectDirty = true;
                }

                if (GUILayout.Button("Replace level"))
                {
                    LeaderboardDebuggerService.ReplaceLevel(level, LeaderboardGenerator.Generate(generateCount, distribution, minTime, Mathf.Max(minTime, maxTime)));
                    inspectDirty = true;
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Single entry", EditorStyles.boldLabel);
            singleName = EditorGUILayout.TextField("Name", singleName);

            using (new EditorGUILayout.HorizontalScope())
            {
                singleId = EditorGUILayout.TextField("Id", singleId);
                if (GUILayout.Button("New GUID", GUILayout.Width(80))) singleId = Guid.NewGuid().ToString();
            }

            singleTime = EditorGUILayout.FloatField("Time", singleTime);

            if (GUILayout.Button("Add entry"))
            {
                var id = string.IsNullOrEmpty(singleId) ? Guid.NewGuid().ToString() : singleId;
                LeaderboardDebuggerService.AddEntries(level, new[] { new Score { name = singleName, id = id, timescore = singleTime } });
                inspectDirty = true;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Ties", EditorStyles.boldLabel);
            tieCount = Mathf.Max(0, EditorGUILayout.IntField("Entries with the same time", tieCount));
            tieTime = EditorGUILayout.FloatField("Shared time", tieTime);

            if (GUILayout.Button("Create ties"))
            {
                LeaderboardDebuggerService.AddEntries(level, LeaderboardGenerator.CreateTies(tieCount, tieTime));
                inspectDirty = true;
            }
        }

        private void DrawInspect()
        {
            if (inspectDirty) RefreshInspect();

            if (GUILayout.Button("Refresh")) RefreshInspect();

            EditorGUILayout.LabelField("Save file", LeaderboardDebuggerService.SavePath);

            if (inspectError != null)
            {
                EditorGUILayout.HelpBox(inspectError, MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Levels in save", inspectLevels.Count == 0 ? "none" : string.Join(", ", inspectLevels));
            EditorGUILayout.LabelField($"Level {level} entries (sorted as the game sorts them)", EditorStyles.boldLabel);

            if (inspectEntries.Count == 0) EditorGUILayout.HelpBox($"Level {level} has no entries.", MessageType.Info);

            for (var i = 0; i < inspectEntries.Count; i++)
            {
                var entry = inspectEntries[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"#{i + 1}", GUILayout.Width(40));
                    EditorGUILayout.LabelField(entry.name, GUILayout.MinWidth(80));
                    EditorGUILayout.LabelField(entry.id, GUILayout.MinWidth(80));
                    EditorGUILayout.LabelField(LeaderboardDebuggerService.FormatTime(entry.timescore), GUILayout.Width(100));
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Raw saved data", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(inspectDescription, EditorStyles.textArea, GUILayout.MinHeight(Mathf.Min(300, 16 * (inspectDescription.Count(c => c == '\n') + 2))));
            showHex = EditorGUILayout.Foldout(showHex, "Hex dump of the binary file");
            if (showHex) EditorGUILayout.SelectableLabel(inspectHex, EditorStyles.textArea, GUILayout.MinHeight(300));
        }

        private void RefreshInspect()
        {
            inspectDirty = false;
            inspectEntries = new List<Score>();
            inspectLevels = new List<int>();
            inspectDescription = "";
            inspectHex = "";

            if (!LeaderboardDebuggerService.TryReadWithoutRepair(out var data, out inspectError)) return;

            inspectLevels = LeaderboardDebuggerService.GetLevels(data);
            inspectEntries = LeaderboardDebuggerService.GetEntries(data, level);
            inspectDescription = LeaderboardDebuggerService.Describe(data);
            inspectHex = LeaderboardDebuggerService.HexDump(File.ReadAllBytes(LeaderboardDebuggerService.SavePath), 4096);
        }

        private void DrawChecks()
        {
            if (GUILayout.Button("Run all checks (every tab)"))
            {
                foreach (var result in LeaderboardChecks.ExecuteAll(LeaderboardChecks.Everything(level))) results[result.Name] = result;
                inspectDirty = true;
            }

            var everything = LeaderboardChecks.Everything(level).Select(check => check.Name).ToList();
            var known = results.Values.Where(result => everything.Contains(result.Name)).ToList();
            EditorGUILayout.LabelField($"Passed: {known.Count(result => result.Passed)}   Failed: {known.Count(result => !result.Passed)}   Not run: {everything.Count - known.Count}", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawCheckList("Correctness", LeaderboardChecks.Correctness(level));
        }

        private void DrawPersistence()
        {
            DrawCheckList("Persistence", LeaderboardChecks.Persistence());
        }

        private void DrawCheckList(string title, List<Check> checks)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                if (GUILayout.Button($"Run all {title.ToLower()}", GUILayout.Width(180)))
                {
                    foreach (var result in LeaderboardChecks.ExecuteAll(checks)) results[result.Name] = result;
                    inspectDirty = true;
                }
            }

            foreach (var check in checks)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Run", GUILayout.Width(40)))
                    {
                        results[check.Name] = LeaderboardChecks.Execute(check);
                        inspectDirty = true;
                    }

                    EditorGUILayout.LabelField(check.Name);
                }

                if (results.TryGetValue(check.Name, out var shown))
                {
                    EditorGUILayout.HelpBox($"{(shown.Passed ? "PASS" : "FAIL")}: {shown.Message}", shown.Passed ? MessageType.Info : MessageType.Error);
                }
            }
        }

        private void DrawData()
        {
            EditorGUILayout.LabelField("Backup (stored in Library, survives domain reloads)", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Backup", LeaderboardDebuggerService.HasLibraryBackup ? LeaderboardDebuggerService.LibraryBackupTime.ToString("g") : "none");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Back up current data"))
                {
                    LeaderboardDebuggerService.BackupToLibrary();
                }

                using (new EditorGUI.DisabledScope(!LeaderboardDebuggerService.HasLibraryBackup))
                {
                    if (GUILayout.Button("Restore backup"))
                    {
                        LeaderboardDebuggerService.RestoreFromLibrary();
                        inspectDirty = true;
                    }

                    if (GUILayout.Button("Delete backup")) LeaderboardDebuggerService.DeleteLibraryBackup();
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Snapshots", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save snapshot to file"))
                {
                    var path = EditorUtility.SaveFilePanel("Save leaderboard snapshot", "", "leaderboard_snapshot", "save");
                    if (!string.IsNullOrEmpty(path)) LeaderboardDebuggerService.SaveSnapshot(path);
                }

                if (GUILayout.Button("Load snapshot from file"))
                {
                    var path = EditorUtility.OpenFilePanel("Load leaderboard snapshot", "", "save");
                    if (!string.IsNullOrEmpty(path))
                    {
                        LeaderboardDebuggerService.LoadSnapshot(path);
                        inspectDirty = true;
                    }
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Clear", EditorStyles.boldLabel);

            if (GUILayout.Button("Clear all leaderboard data"))
            {
                if (EditorUtility.DisplayDialog("Clear leaderboard", "This deletes every entry on every level from the saved leaderboard. Continue?", "Clear", "Cancel"))
                {
                    LeaderboardDebuggerService.ClearAll();
                    inspectDirty = true;
                }
            }
        }
    }
}
