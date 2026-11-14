using Game.Levels;

namespace Game.Tools.DebugCommands
{
    /// <summary>
    /// Game systems the console commands act on, so the commands can run against fakes in tests.
    /// </summary>
    public interface IConsoleServices
    {
        bool IsPlaying { get; }
        bool GhostEnabled { get; }

        void LoadGameMode(GameModeSO mode, BaseLevelSO level);
        void SetGhostEnabled(bool enabled);
        void ClearSettingsData();
        void ClearSaveData();
        void ClearLeaderboardData();
    }
}
