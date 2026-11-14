using System;
using UnityEngine;

namespace Game.ConsoleCommands.Tests
{
    /// <summary>
    /// Implemented next to the console code so the PlayMode tests can reach it without referencing Assembly-CSharp.
    /// Creating one backs up the save data and logs in; disposing it restores everything.
    /// </summary>
    public interface IConsoleCommandsTestBackend : IDisposable
    {
        string LoadableGameModeId { get; }
        string MissingScenesReason { get; }
        bool IsLoading { get; }
        string CurrentGameModeId { get; }
        string CurrentLevelId { get; }
        bool GhostEnabled { get; }

        string FirstLevelId(string gameModeId);
        bool Execute(string input, out string message);
        GameObject SpawnGhostPlayer();
        bool IsGhostPlayerEnabled(GameObject ghost);
    }
}
