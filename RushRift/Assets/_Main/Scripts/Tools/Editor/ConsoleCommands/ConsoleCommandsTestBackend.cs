#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.IO;
using System.Linq;
using _Main.Scripts.Ghost;
using Game.ConsoleCommands.Tests;
using Game.Levels;
using Game.Tools.DebugCommands;
using UnityEditor;
using UnityEngine;

namespace Game.ConsoleCommands
{
    /// <summary>
    /// Connects the console PlayMode tests to ConsoleCommandSet, GameEntry and GhostPlayer, with the save data backed up.
    /// </summary>
    public sealed class ConsoleCommandsTestBackend : IConsoleCommandsTestBackend
    {
        private const string Password = "console-test";
        private const string PreferredMode = "SectorsGameMode";

        private readonly ConsoleTestDataBackup _backup;
        private readonly List<GameModeSO> _modes;
        private readonly ConsoleCommandSet _commands;
        private readonly List<GameObject> _spawned = new();

        public ConsoleCommandsTestBackend()
        {
            _backup = new ConsoleTestDataBackup();
            _modes = AssetDatabase.FindAssets($"t:{nameof(GameModeSO)}")
                .Select(guid => AssetDatabase.LoadAssetAtPath<GameModeSO>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(mode => mode && mode.Levels != null && mode.Levels.Count > 0 && mode.Levels[0])
                .OrderBy(mode => mode.name)
                .ToList();
            _commands = new ConsoleCommandSet(new ConsoleSession(Password), new GameConsoleServices(), _modes);
            _commands.TryExecute($"login {Password}", out _);
        }

        public string LoadableGameModeId
        {
            get
            {
                var mode = _modes.FirstOrDefault(candidate => candidate.name == PreferredMode) ?? _modes.FirstOrDefault();
                return mode ? mode.name : null;
            }
        }

        public string MissingScenesReason
        {
            get
            {
                var mode = FindMode(LoadableGameModeId);
                if (!mode) return "No game mode to load.";

                var inBuild = EditorBuildSettings.scenes
                    .Where(scene => scene.enabled)
                    .Select(scene => Path.GetFileNameWithoutExtension(scene.path))
                    .ToHashSet();

                var needed = mode.Levels[0].GetSceneLoadRequests()
                    .Select(request => request.SceneName)
                    .Append(GameEntry.MAIN_SCENE)
                    .Where(scene => !string.IsNullOrEmpty(scene));

                var missing = needed.Where(scene => !inBuild.Contains(scene)).ToList();
                return missing.Count == 0 ? null : $"Scenes missing from Build Settings: {string.Join(", ", missing)}";
            }
        }

        public bool IsLoading => GameEntry.LoadingState.Loading;
        public string CurrentGameModeId => GlobalLevelManager.CurrentMode.TryGet(out var mode) ? mode.name : null;
        public string CurrentLevelId => GlobalLevelManager.CurrentLevel.TryGet(out var level) ? level.name : null;
        public bool GhostEnabled => GhostPlayer.GhostEnabled;

        public string FirstLevelId(string gameModeId)
        {
            var mode = FindMode(gameModeId);
            return mode ? mode.Levels[0].name : null;
        }

        public bool Execute(string input, out string message)
        {
            if (!_commands.TryExecute(input, out var result))
            {
                message = $"'{input}' is not one of the new console commands.";
                return false;
            }

            message = result.Message;
            return result.Success;
        }

        public GameObject SpawnGhostPlayer()
        {
            var ghost = new GameObject("ConsoleCommandsTestGhost");
            _spawned.Add(ghost);
            ghost.AddComponent<GhostPlayer>();
            return ghost;
        }

        public bool IsGhostPlayerEnabled(GameObject ghost)
        {
            return ghost && ghost.TryGetComponent<GhostPlayer>(out var player) && player.enabled;
        }

        public void Dispose()
        {
            GhostPlayer.SetGhostEnabled(true);

            foreach (var ghost in _spawned.Where(ghost => ghost))
            {
                if (Application.isPlaying) Object.Destroy(ghost);
                else Object.DestroyImmediate(ghost);
            }

            _spawned.Clear();
            _backup.Dispose();
        }

        private GameModeSO FindMode(string id)
        {
            return _modes.FirstOrDefault(mode => mode.name == id);
        }
    }
}
#endif
