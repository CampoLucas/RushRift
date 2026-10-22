using System.Linq;
using Game.Tools.DebugCommands;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.ConsoleCommands.Tests
{
    /// <summary>
    /// Login, logout and the login gate in front of every console command.
    /// </summary>
    public class ConsoleAuthTests
    {
        private const string Password = "Secret123";

        private static readonly string[] ProtectedCommands =
        {
            "logout",
            "clear",
            "load-gm SomeMode",
            "disable-ghost",
            "enable-ghost",
            "clear-settings-data",
            "clear-save-data",
            "clear-leaderboard-data"
        };

        private static readonly string[] LegacyCommands = { "help", "dash_hack", "kill_all", "set_medal 1", "toggle_pause_ui" };

        private FakeConsoleServices services;
        private ConsoleSession session;
        private ConsoleCommandSet commands;

        [SetUp]
        public void SetUp()
        {
            services = new FakeConsoleServices();
            session = new ConsoleSession(Password);
            commands = new ConsoleCommandSet(session, services, null);
        }

        private ConsoleResult Run(string input)
        {
            Assert.IsTrue(commands.TryExecute(input, out var result), $"'{input}' was not handled by the command set");
            return result;
        }

        private void LogIn()
        {
            Assert.IsTrue(Run($"login {Password}").Success);
        }

        [Test]
        public void Login_CorrectPassword_LogsIn()
        {
            var result = Run($"login {Password}");

            Assert.IsTrue(result.Success);
            Assert.IsTrue(session.LoggedIn);
        }

        [Test]
        public void Login_WrongPassword_FailsAndStaysLoggedOut()
        {
            var result = Run("login nope");

            Assert.IsFalse(result.Success);
            StringAssert.Contains("Wrong password", result.Message);
            Assert.IsFalse(session.LoggedIn);
        }

        [Test]
        public void Login_PasswordIsCaseSensitive()
        {
            Assert.IsFalse(Run($"login {Password.ToUpperInvariant()}").Success);
            Assert.IsFalse(session.LoggedIn);
        }

        [Test]
        public void Login_NoArgument_GivesUsageError()
        {
            var result = Run("login");

            Assert.IsFalse(result.Success);
            StringAssert.Contains("Usage", result.Message);
            Assert.IsFalse(session.LoggedIn);
        }

        [Test]
        public void Login_TooManyArguments_GivesUsageError()
        {
            var result = Run($"login {Password} extra");

            Assert.IsFalse(result.Success);
            StringAssert.Contains("Usage", result.Message);
            Assert.IsFalse(session.LoggedIn);
        }

        [TestCase(null)]
        [TestCase("")]
        public void Login_NoPasswordConfigured_AlwaysFails(string configured)
        {
            session = new ConsoleSession(configured);
            commands = new ConsoleCommandSet(session, services, null);

            var result = Run("login anything");

            Assert.IsFalse(result.Success);
            StringAssert.Contains("No console password", result.Message);
            Assert.IsFalse(session.LoggedIn);
        }

        [Test]
        public void Login_ConfigWithoutPassword_Fails()
        {
            var config = ScriptableObject.CreateInstance<ConsoleConfigSO>();
            commands = ConsoleCommandSet.Create(config, session, services);

            Assert.IsFalse(Run($"login {Password}").Success);

            Object.DestroyImmediate(config);
        }

        [Test]
        public void Login_NoConfig_Fails()
        {
            commands = ConsoleCommandSet.Create(null, session, services);

            Assert.IsFalse(Run($"login {Password}").Success);
        }

        [Test]
        public void Login_UsesPasswordFromConfig()
        {
            var config = ScriptableObject.CreateInstance<ConsoleConfigSO>();
            var serialized = new SerializedObject(config);
            serialized.FindProperty("password").stringValue = "FromConfig";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            commands = ConsoleCommandSet.Create(config, session, services);

            Assert.IsFalse(Run($"login {Password}").Success);
            Assert.IsTrue(Run("login FromConfig").Success);

            Object.DestroyImmediate(config);
        }

        [Test]
        public void Login_WhenAlreadyLoggedIn_StaysLoggedIn()
        {
            LogIn();

            Assert.IsTrue(Run($"login {Password}").Success);
            Assert.IsFalse(Run("login wrong").Success);
            Assert.IsTrue(session.LoggedIn);
        }

        [Test]
        public void Logout_LocksAgain()
        {
            LogIn();

            Assert.IsTrue(Run("logout").Success);
            Assert.IsFalse(session.LoggedIn);

            var result = Run("clear-settings-data");
            Assert.IsFalse(result.Success);
            Assert.AreEqual(0, services.Calls);
        }

        [Test]
        public void Logout_WhenLoggedOut_SaysNotLoggedIn()
        {
            var result = Run("logout");

            Assert.IsFalse(result.Success);
            StringAssert.Contains("Not logged in", result.Message);
        }

        [TestCaseSource(nameof(ProtectedCommands))]
        public void ProtectedCommand_LoggedOut_IsRefusedAndDoesNothing(string input)
        {
            var result = Run(input);

            Assert.IsFalse(result.Success);
            StringAssert.Contains("Not logged in", result.Message);
            Assert.AreEqual(0, services.Calls);
        }

        [TestCase("disable-ghost")]
        [TestCase("enable-ghost")]
        [TestCase("clear-settings-data")]
        [TestCase("clear-save-data")]
        [TestCase("clear-leaderboard-data")]
        public void ProtectedCommand_LoggedIn_Runs(string input)
        {
            LogIn();

            var result = Run(input);

            Assert.IsTrue(result.Success, result.Message);
            Assert.AreEqual(1, services.Calls);
        }

        [TestCaseSource(nameof(LegacyCommands))]
        public void LegacyCommand_LoggedOut_IsRefused(string input)
        {
            var result = Run(input);

            Assert.IsFalse(result.Success);
            StringAssert.Contains("Not logged in", result.Message);
        }

        [TestCaseSource(nameof(LegacyCommands))]
        public void LegacyCommand_LoggedIn_IsPassedToTheOldConsole(string input)
        {
            LogIn();

            Assert.IsFalse(commands.TryExecute(input, out _));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("    ")]
        public void EmptyInput_DoesNotThrow(string input)
        {
            Assert.DoesNotThrow(() => commands.TryExecute(input, out _));
            LogIn();
            Assert.DoesNotThrow(() => commands.TryExecute(input, out _));
        }

        [Test]
        public void CommandNames_IgnoreCaseAndExtraSpaces()
        {
            Assert.IsTrue(Run($"   LOGIN    {Password}   ").Success);
            Assert.IsTrue(Run("  Clear-Settings-Data ").Success);
            Assert.AreEqual(1, services.SettingsClears);
        }

        [Test]
        public void Clear_LoggedIn_ClearsHistoryOnceAndStaysLoggedIn()
        {
            LogIn();
            var cleared = 0;
            session.HistoryCleared += () => cleared++;

            var result = Run("clear");

            Assert.IsTrue(result.Success, result.Message);
            Assert.AreEqual(1, cleared);
            Assert.IsTrue(session.LoggedIn);
            Assert.AreEqual(0, services.Calls);
        }

        [Test]
        public void Clear_LoggedOut_DoesNotClearHistory()
        {
            var cleared = 0;
            session.HistoryCleared += () => cleared++;

            var result = Run("clear");

            Assert.IsFalse(result.Success);
            Assert.AreEqual(0, cleared);
        }

        [Test]
        public void Logout_ClearsHistory()
        {
            LogIn();
            var cleared = 0;
            session.HistoryCleared += () => cleared++;

            Assert.IsTrue(Run("logout").Success);

            Assert.AreEqual(1, cleared);
        }

        [Test]
        public void WrongPasswordOrFailedLogout_DoNotClearHistory()
        {
            var cleared = 0;
            session.HistoryCleared += () => cleared++;

            Run("login wrong");
            Run("logout");

            Assert.AreEqual(0, cleared);
        }

        [TestCase("clear all")]
        [TestCase("logout now")]
        [TestCase("disable-ghost please")]
        [TestCase("enable-ghost 1")]
        [TestCase("clear-settings-data all")]
        [TestCase("clear-save-data all")]
        [TestCase("clear-leaderboard-data all")]
        public void NoArgumentCommand_WithArguments_GivesUsageError(string input)
        {
            LogIn();

            var result = Run(input);

            Assert.IsFalse(result.Success);
            StringAssert.Contains("Usage", result.Message);
            Assert.AreEqual(0, services.Calls);
            Assert.IsTrue(session.LoggedIn);
        }

        [Test]
        public void ServiceException_BecomesErrorResult()
        {
            LogIn();
            services.ThrowOnCall = true;

            ConsoleResult result = default;
            Assert.DoesNotThrow(() => commands.TryExecute("clear-save-data", out result));
            Assert.IsFalse(result.Success);
            StringAssert.Contains("Fake failure", result.Message);
        }

        [TestCase("login secret", "login ••••••")]
        [TestCase("LOGIN ab", "LOGIN ••")]
        [TestCase("login ", "login ")]
        [TestCase("login", "login")]
        [TestCase("loginx abc", "loginx abc")]
        [TestCase("dash_hack", "dash_hack")]
        [TestCase(null, null)]
        public void MaskInput_HidesOnlyThePassword(string input, string expected)
        {
            Assert.AreEqual(expected, ConsoleCommandSet.MaskInput(input));
        }

        [Test]
        public void Set_HasAllNewCommands()
        {
            var ids = commands.Commands.Select(command => command.ID).ToArray();

            CollectionAssert.AreEquivalent(new[] { "login", "logout", "clear", "load-gm", "disable-ghost", "enable-ghost", "clear-settings-data", "clear-save-data", "clear-leaderboard-data" }, ids);
        }
    }
}
