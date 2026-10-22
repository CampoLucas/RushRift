using System.Collections.Generic;
using _Main.Scripts.Ghost;
using Game.Levels;
using Game.Saves;
using UnityEngine;

namespace Game.Tools.DebugCommands
{
    /// <summary>
    /// Console services backed by GameEntry, GhostPlayer and SaveSystem.
    /// </summary>
    public sealed class GameConsoleServices : IConsoleServices
    {
        public bool IsPlaying => Application.isPlaying;
        public bool GhostEnabled => GhostPlayer.GhostEnabled;

        public void LoadGameMode(GameModeSO mode, BaseLevelSO level)
        {
            Time.timeScale = 1f;
            GameEntry.LoadSessionAsync(mode, level);
        }

        public void SetGhostEnabled(bool enabled)
        {
            GhostPlayer.SetGhostEnabled(enabled);
        }

        public void ClearSettingsData()
        {
            SaveSystem.ResetSettings();
        }

        public void ClearSaveData()
        {
            SaveSystem.ResetGame();
        }

        public void ClearLeaderboardData()
        {
            SaveSystem.ResetLeaderBoard();
            RefreshLeaderboards();
        }

        private static void RefreshLeaderboards()
        {
            if (!Application.isPlaying) return;

            foreach (var presenter in Object.FindObjectsOfType<LeaderboardPresenter>())
            {
                try
                {
                    presenter.GetScoreData();
                }
                catch (KeyNotFoundException)
                {
                }
            }
        }
    }
}
