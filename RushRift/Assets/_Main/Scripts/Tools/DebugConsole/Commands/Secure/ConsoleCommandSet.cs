using System;
using System.Collections.Generic;
using System.Linq;
using Game.Levels;
using UnityEngine;

namespace Game.Tools.DebugCommands
{
    /// <summary>
    /// Parses and runs the login protected console commands, and refuses every other command while logged out.
    /// </summary>
    public sealed class ConsoleCommandSet
    {
        public const string LoginId = "login";
        public const char MaskCharacter = '•';

        public static ConsoleResult NotLoggedIn => ConsoleResult.Error("Not logged in. Use: login <password>");

        public ConsoleSession Session { get; }
        public IConsoleServices Services { get; }
        public IReadOnlyList<GameModeSO> GameModes { get; }
        public IReadOnlyList<SecureDebugCommand> Commands { get; }
        public IReadOnlyList<string> GameModeIds => GameModes.Where(mode => mode).Select(mode => mode.name).ToList();

        public ConsoleCommandSet(ConsoleSession session, IConsoleServices services, IEnumerable<GameModeSO> gameModes)
        {
            Session = session;
            Services = services;
            GameModes = gameModes != null ? gameModes.ToList() : new List<GameModeSO>();
            Commands = new List<SecureDebugCommand>
            {
                new LoginCmd(session, services),
                new LogoutCmd(session, services),
                new ClearCmd(session, services),
                new LoadGameModeCmd(session, services, GameModes),
                new DisableGhostCmd(session, services),
                new EnableGhostCmd(session, services),
                new ClearSettingsDataCmd(session, services),
                new ClearSaveDataCmd(session, services),
                new ClearLeaderboardDataCmd(session, services)
            };
        }

        public static ConsoleCommandSet Create(ConsoleConfigSO config, ConsoleSession session, IConsoleServices services)
        {
            session.SetPassword(config ? config.Password : null);
            return new ConsoleCommandSet(session, services, config ? config.GameModes : null);
        }

        public bool TryExecute(string input, out ConsoleResult result)
        {
            var parts = Split(input);

            if (parts.Length == 0)
            {
                result = Session.LoggedIn ? ConsoleResult.Error("Type a command.") : NotLoggedIn;
                Print(result);
                return true;
            }

            var command = Commands.FirstOrDefault(candidate => string.Equals(candidate.ID, parts[0], StringComparison.OrdinalIgnoreCase));

            if (command != null)
            {
                result = command.Execute(parts.Skip(1).ToArray());
                Print(result);
                return true;
            }

            if (!Session.LoggedIn)
            {
                result = NotLoggedIn;
                Print(result);
                return true;
            }

            result = default;
            return false;
        }

        public SecureDebugCommand Find(string id)
        {
            return Commands.FirstOrDefault(command => string.Equals(command.ID, id, StringComparison.OrdinalIgnoreCase));
        }

        public static string[] Split(string input)
        {
            return string.IsNullOrWhiteSpace(input) ? Array.Empty<string>() : input.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        }

        public static bool IsLoginInput(string input)
        {
            return LoginPrefixLength(input) > 0;
        }

        public static int LoginPrefixLength(string input)
        {
            if (string.IsNullOrEmpty(input)) return 0;

            var start = input.Length - input.TrimStart().Length;
            var end = start + LoginId.Length;

            if (input.Length <= end || !char.IsWhiteSpace(input[end])) return 0;
            if (string.Compare(input, start, LoginId, 0, LoginId.Length, StringComparison.OrdinalIgnoreCase) != 0) return 0;

            return end + 1;
        }

        public static string MaskInput(string input)
        {
            var prefix = LoginPrefixLength(input);
            return prefix == 0 ? input : input.Substring(0, prefix) + new string(MaskCharacter, input.Length - prefix);
        }

        public static void Print(ConsoleResult result)
        {
            if (result.Success) Debug.Log($"[Console] {result.Message}");
            else Debug.LogWarning($"[Console] {result.Message}");
        }
    }
}
