using System;
using Game.Levels;
using Game.Tools.DebugCommands;

namespace Game.ConsoleCommands.Tests
{
    /// <summary>
    /// Records what the console commands asked for instead of touching the game.
    /// </summary>
    public sealed class FakeConsoleServices : IConsoleServices
    {
        public bool IsPlaying { get; set; } = true;
        public bool GhostEnabled { get; private set; } = true;
        public bool ThrowOnCall { get; set; }

        public int Calls { get; private set; }
        public int LoadCalls { get; private set; }
        public int GhostCalls { get; private set; }
        public int SettingsClears { get; private set; }
        public int SaveClears { get; private set; }
        public int LeaderboardClears { get; private set; }
        public GameModeSO LoadedMode { get; private set; }
        public BaseLevelSO LoadedLevel { get; private set; }

        public void LoadGameMode(GameModeSO mode, BaseLevelSO level)
        {
            Record();
            LoadCalls++;
            LoadedMode = mode;
            LoadedLevel = level;
        }

        public void SetGhostEnabled(bool enabled)
        {
            Record();
            GhostCalls++;
            GhostEnabled = enabled;
        }

        public void ClearSettingsData()
        {
            Record();
            SettingsClears++;
        }

        public void ClearSaveData()
        {
            Record();
            SaveClears++;
        }

        public void ClearLeaderboardData()
        {
            Record();
            LeaderboardClears++;
        }

        private void Record()
        {
            Calls++;
            if (ThrowOnCall) throw new InvalidOperationException("Fake failure");
        }
    }
}
