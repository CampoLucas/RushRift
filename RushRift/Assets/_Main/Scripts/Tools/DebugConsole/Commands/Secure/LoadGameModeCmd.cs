using System;
using System.Collections.Generic;
using System.Linq;
using Game.Levels;

namespace Game.Tools.DebugCommands
{
    /// <summary>
    /// Starts a session of a game mode from the console config on its first level. The id is the game mode asset name.
    /// </summary>
    public class LoadGameModeCmd : SecureDebugCommand
    {
        public override bool RequiresPlayMode => true;

        private readonly IReadOnlyList<GameModeSO> _modes;

        public LoadGameModeCmd(ConsoleSession session, IConsoleServices services, IReadOnlyList<GameModeSO> modes)
            : base("load-gm", "Loads the first level of a game mode", "load-gm <gamemode id>", 1, session, services)
        {
            _modes = modes ?? Array.Empty<GameModeSO>();
        }

        protected override ConsoleResult Run(string[] args)
        {
            var modes = _modes.Where(mode => mode).ToList();
            if (modes.Count == 0) return ConsoleResult.Error("No game modes are set in the console config.");

            var mode = modes.FirstOrDefault(candidate => string.Equals(candidate.name, args[0], StringComparison.OrdinalIgnoreCase));
            if (!mode) return ConsoleResult.Error($"Unknown game mode '{args[0]}'. Valid ids: {string.Join(", ", modes.Select(candidate => candidate.name))}");

            var level = mode.Levels != null && mode.Levels.Count > 0 ? mode.Levels[0] : null;
            if (!level) return ConsoleResult.Error($"Game mode '{mode.name}' has no first level.");

            Services.LoadGameMode(mode, level);
            return ConsoleResult.Ok($"Loading {mode.name} ({level.name}).");
        }
    }
}
