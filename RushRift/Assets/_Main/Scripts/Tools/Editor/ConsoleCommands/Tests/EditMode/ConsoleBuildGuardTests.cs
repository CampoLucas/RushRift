using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Tools.DebugCommands;
using NUnit.Framework;
using UnityEngine;

namespace Game.ConsoleCommands.Tests
{
    /// <summary>
    /// Makes sure the new commands are not hidden behind a scripting define, so they exist in every build.
    /// </summary>
    public class ConsoleBuildGuardTests
    {
        private static readonly string[] GuardSymbols = { "LEADERBOARD_CMD_ENABLED", "CHEATS_ENABLED" };

        private static string ScriptsFolder => Path.Combine(Application.dataPath, "_Main", "Scripts", "Tools", "DebugConsole");

        [Test]
        public void SecureCommandFiles_HaveNoConditionalCompilation()
        {
            var files = Directory.GetFiles(Path.Combine(ScriptsFolder, "Commands", "Secure"), "*.cs");

            Assert.IsNotEmpty(files);

            foreach (var file in files)
            {
                var directive = File.ReadAllLines(file).FirstOrDefault(line => line.TrimStart().StartsWith("#if"));
                Assert.IsNull(directive, $"{Path.GetFileName(file)} contains '{directive}'");
            }
        }

        [TestCase("Commands.TryExecute")]
        [TestCase("ConsoleCommandSet.Create")]
        [TestCase("ConsoleCommandSet.MaskInput")]
        public void DebugConsoleDispatch_IsOutsideGuardedBlocks(string marker)
        {
            var open = new Stack<string>();
            var found = false;

            foreach (var raw in File.ReadAllLines(Path.Combine(ScriptsFolder, "DebugConsole.cs")))
            {
                var line = raw.Trim();

                if (line.StartsWith("#if")) open.Push(line);
                else if (line.StartsWith("#endif")) open.Pop();
                else if (line.Contains(marker))
                {
                    found = true;
                    var guard = open.FirstOrDefault(condition => GuardSymbols.Any(condition.Contains));
                    Assert.IsNull(guard, $"'{marker}' is inside '{guard}'");
                }
            }

            Assert.IsTrue(found, $"'{marker}' was not found in DebugConsole.cs");
        }

        [Test]
        public void Commands_ExistWithTheCurrentDefines()
        {
            var commands = new ConsoleCommandSet(new ConsoleSession("pw"), new FakeConsoleServices(), null);

            Assert.AreEqual(8, commands.Commands.Count);
        }
    }
}
