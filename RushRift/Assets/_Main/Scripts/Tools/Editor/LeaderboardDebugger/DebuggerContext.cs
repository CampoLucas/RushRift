using System;
using System.Collections.Generic;
using System.IO;
using Game.Saves;
using UnityEditor;
using UnityEngine;

namespace Game.LeaderboardDebugger
{
    public enum LevelMode
    {
        Current,
        Listed,
        Custom
    }

    /// <summary>
    /// Level selection, live save data and help settings shared by every tab of the debugger window.
    /// </summary>
    [Serializable]
    public sealed class DebuggerContext
    {
        private const string ShowHelpKey = "LeaderboardDebugger.ShowHelp";

        [SerializeField] private LevelMode mode = LevelMode.Current;
        [SerializeField] private int manualLevel = 1;
        [SerializeField] private bool showHelp = true;

        private long stamp = -1;
        private int stampLevel = int.MinValue;

        public LevelMode Mode => mode;
        public int ManualLevel => manualLevel;
        public int Level { get; private set; } = 1;
        public string LevelSource { get; private set; } = "";
        public bool LevelResolved { get; private set; } = true;
        public bool SaveExists { get; private set; }
        public string SaveError { get; private set; }
        public LeaderBoardData Data { get; private set; }
        public List<Score> Entries { get; private set; } = new List<Score>();
        public List<int> Levels { get; private set; } = new List<int>();
        public int Revision { get; private set; }

        public bool HasValidLevel => Level >= 0 && LevelResolved;

        public bool ShowHelp
        {
            get => showHelp;
            set
            {
                if (showHelp == value) return;

                showHelp = value;
                EditorPrefs.SetBool(ShowHelpKey, value);
            }
        }

        public void LoadPreferences()
        {
            showHelp = EditorPrefs.GetBool(ShowHelpKey, true);
        }

        public void SetSelection(LevelMode newMode, int levelId)
        {
            mode = newMode;
            if (newMode != LevelMode.Current) manualLevel = levelId;
            Invalidate();
        }

        public void SetManualLevel(int levelId)
        {
            manualLevel = levelId;
            Invalidate();
        }

        public void Invalidate()
        {
            stamp = -1;
        }

        public bool Poll()
        {
            LevelResolved = LeaderboardLevelSource.TryResolve(mode == LevelMode.Current, manualLevel, out var resolved, out var source);
            Level = resolved;
            LevelSource = mode == LevelMode.Current ? source : mode == LevelMode.Listed ? "selected level" : "custom id";

            var info = new FileInfo(LeaderboardDebuggerService.SavePath);
            var currentStamp = info.Exists ? info.LastWriteTimeUtc.Ticks ^ info.Length : 0L;
            SaveExists = info.Exists;

            if (currentStamp == stamp && Level == stampLevel) return false;

            stamp = currentStamp;
            stampLevel = Level;
            Revision++;
            Data = null;
            Entries = new List<Score>();
            Levels = new List<int>();

            if (!LeaderboardDebuggerService.TryReadWithoutRepair(out var read, out var error))
            {
                SaveError = error;
                return true;
            }

            SaveError = null;
            Data = read;
            Entries = LeaderboardDebuggerService.GetEntries(read, Level);
            Levels = LeaderboardDebuggerService.GetLevels(read);
            return true;
        }
    }
}
