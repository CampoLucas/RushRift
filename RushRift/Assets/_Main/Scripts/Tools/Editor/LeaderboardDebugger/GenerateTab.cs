using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.LeaderboardDebugger
{
    /// <summary>
    /// Generate tab: adds random, single and tied entries to the active level.
    /// </summary>
    [Serializable]
    public sealed class GenerateTab
    {
        private const int LargeCount = 10000;

        [SerializeField] private int generateCount = 50;
        [SerializeField] private ScoreDistribution distribution = ScoreDistribution.Uniform;
        [SerializeField] private float minTime = 20f;
        [SerializeField] private float maxTime = 300f;
        [SerializeField] private string singleName = "TestPlayer";
        [SerializeField] private string singleId = "";
        [SerializeField] private float singleTime = 60f;
        [SerializeField] private int tieCount = 5;
        [SerializeField] private float tieTime = 60f;

        public void Draw(DebuggerContext context)
        {
            DebuggerHelp.Info(context, "Adds fake entries to the saved leaderboard of the active level. Use it to fill a board quickly or to build specific cases such as ties.");

            DrawRandom(context);
            EditorGUILayout.Space();
            DrawSingle(context);
            EditorGUILayout.Space();
            DrawTies(context);
        }

        private void DrawRandom(DebuggerContext context)
        {
            EditorGUILayout.LabelField("Random entries", EditorStyles.boldLabel);

            generateCount = Mathf.Max(0, EditorGUILayout.IntField("Count", generateCount));
            if (generateCount == 0) DebuggerHelp.Warning("Count is 0, so nothing will be added.");
            else if (generateCount > LargeCount) DebuggerHelp.Warning($"{generateCount} entries is a lot. The window may freeze while it saves.");

            distribution = (ScoreDistribution)EditorGUILayout.EnumPopup("Distribution", distribution);
            DebuggerHelp.Info(context, "Uniform spreads times evenly between Min and Max. Normal clusters them around the middle. Long tail makes most times fast with a few very slow ones.");

            minTime = EditorGUILayout.FloatField("Min time", minTime);
            maxTime = EditorGUILayout.FloatField("Max time", maxTime);
            DebuggerHelp.Info(context, "Times are in seconds. In this leaderboard a lower time ranks higher.");
            if (minTime < 0f) DebuggerHelp.Warning("A negative Min time creates entries with negative times, which the game never produces.");
            if (maxTime < minTime) DebuggerHelp.Warning("Max time is lower than Min time, so every generated time will equal Min time.");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add to level"))
                {
                    LeaderboardDebuggerService.AddEntries(context.Level, LeaderboardGenerator.Generate(generateCount, distribution, minTime, Mathf.Max(minTime, maxTime)));
                }

                if (GUILayout.Button("Replace level"))
                {
                    LeaderboardDebuggerService.ReplaceLevel(context.Level, LeaderboardGenerator.Generate(generateCount, distribution, minTime, Mathf.Max(minTime, maxTime)));
                }
            }

            DebuggerHelp.Info(context, "Add to level keeps the existing entries. Replace level deletes the saved entries of the level first.");
            if (context.Entries.Count > 0) DebuggerHelp.Warning($"Replace level will delete the {context.Entries.Count} entries currently saved for level {context.Level}.");
            WarnIfNoLevel(context);
        }

        private void DrawSingle(DebuggerContext context)
        {
            EditorGUILayout.LabelField("Single entry", EditorStyles.boldLabel);

            singleName = EditorGUILayout.TextField("Name", singleName);
            if (string.IsNullOrWhiteSpace(singleName)) DebuggerHelp.Warning("The name is empty, so the entry will show up with a blank name.");

            using (new EditorGUILayout.HorizontalScope())
            {
                singleId = EditorGUILayout.TextField("Id", singleId);
                if (GUILayout.Button("New GUID", GUILayout.Width(80))) singleId = Guid.NewGuid().ToString();
            }

            DebuggerHelp.Info(context, "Entries are stored by id, so reusing an id overwrites that player's entry. Leave it blank to use a random GUID.");
            if (!string.IsNullOrEmpty(singleId) && context.Entries.Any(entry => entry.id == singleId))
            {
                DebuggerHelp.Warning($"An entry with this id already exists on level {context.Level} and will be overwritten.");
            }

            singleTime = EditorGUILayout.FloatField("Time", singleTime);
            if (float.IsNaN(singleTime) || float.IsInfinity(singleTime)) DebuggerHelp.Warning("NaN and infinite times sort unpredictably.");
            else if (singleTime < 0f) DebuggerHelp.Warning("A negative time is never produced by the game.");

            if (GUILayout.Button("Add entry"))
            {
                var id = string.IsNullOrEmpty(singleId) ? Guid.NewGuid().ToString() : singleId;
                LeaderboardDebuggerService.AddEntries(context.Level, new[] { new Score { name = singleName, id = id, timescore = singleTime } });
            }

            WarnIfNoLevel(context);
        }

        private void DrawTies(DebuggerContext context)
        {
            EditorGUILayout.LabelField("Ties", EditorStyles.boldLabel);

            tieCount = Mathf.Max(0, EditorGUILayout.IntField("Entries with the same time", tieCount));
            if (tieCount == 0) DebuggerHelp.Warning("The entry count is 0, so nothing will be added.");

            tieTime = EditorGUILayout.FloatField("Shared time", tieTime);
            DebuggerHelp.Info(context, "Every created entry gets the same time with a different player. The leaderboard has no tie-break rule, so the order of tied entries is not defined.");

            if (GUILayout.Button("Create ties"))
            {
                LeaderboardDebuggerService.AddEntries(context.Level, LeaderboardGenerator.CreateTies(tieCount, tieTime));
            }

            WarnIfNoLevel(context);
        }

        private static void WarnIfNoLevel(DebuggerContext context)
        {
            if (!context.HasValidLevel) DebuggerHelp.Warning($"There is no valid active level (id {context.Level}). Pick a level in the header before adding entries.");
        }
    }
}
