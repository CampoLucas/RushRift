using System;
using System.Collections.Generic;
using System.Linq;
using Game.Saves;

namespace Game.LeaderboardDebugger
{
    public sealed class CheckResult
    {
        public string Name { get; private set; }
        public bool Passed { get; private set; }
        public string Message { get; private set; }

        public CheckResult(string name, bool passed, string message)
        {
            Name = name;
            Passed = passed;
            Message = message;
        }
    }

    public sealed class Check
    {
        public string Name { get; private set; }
        public Func<(bool passed, string message)> Run { get; private set; }

        public Check(string name, Func<(bool passed, string message)> run)
        {
            Name = name;
            Run = run;
        }
    }

    public static class LeaderboardChecks
    {
        public static CheckResult Execute(Check check)
        {
            var backup = LeaderboardDebuggerService.Capture();

            try
            {
                var outcome = check.Run();
                return new CheckResult(check.Name, outcome.passed, outcome.message);
            }
            catch (Exception e)
            {
                return new CheckResult(check.Name, false, $"{e.GetType().Name}: {e.Message}");
            }
            finally
            {
                backup.Restore();
            }
        }

        public static List<CheckResult> ExecuteAll(IEnumerable<Check> checks)
        {
            return checks.Select(Execute).ToList();
        }

        public static List<Check> Everything(int level)
        {
            return Correctness(level).Concat(EdgeCases()).Concat(Persistence()).ToList();
        }

        public static List<Check> Correctness(int level)
        {
            return new List<Check>
            {
                new Check("Sorting: ascending by time (generated data)", SortingGenerated),
                new Check($"Sorting: ascending by time (saved data, level {level})", () => SortingSaved(level)),
                new Check("Ties: no entries lost, order consistent", Ties),
                new Check("Personal best: one entry per player id", OneEntryPerId),
                new Check("Personal best: AddTimeToLevel overwrites without comparing", OverwriteWithoutComparing),
                new Check("Personal best: SaveData.CheckBestTime guard", CheckBestTimeGuard),
                new Check("Levels are independent", LevelsIndependent)
            };
        }

        public static List<Check> EdgeCases()
        {
            return new List<Check>
            {
                new Check("Extreme times: sort order", ExtremeTimesSort),
                new Check("Extreme times: save and reload", ExtremeTimesRoundTrip),
                new Check("Unusual names and ids: save and reload", UnusualNamesRoundTrip),
                new Check("Empty leaderboard: save and reload", EmptyRoundTrip),
                new Check("Empty leaderboard: lookup of a level with no entries", MissingLevelLookup),
                new Check("Single entry: sort and reload", SingleEntry)
            };
        }

        public static List<Check> Persistence()
        {
            return new List<Check>
            {
                new Check("Save and reload: nothing changes", SaveReload),
                new Check("Missing save file: default is created", MissingFile),
                new Check("Corrupted save file: loads without throwing", CorruptedFile),
                new Check("Truncated save file: loads without throwing", TruncatedFile),
                new Check("Manually edited save file: loads without throwing", EditedFile)
            };
        }

        private static (bool, string) Result(string problem, string passMessage)
        {
            return problem == null ? (true, passMessage) : (false, problem);
        }

        private static string FirstProblem(params string[] problems)
        {
            return problems.FirstOrDefault(problem => problem != null);
        }

        private static (bool, string) SortingGenerated()
        {
            var problems = new List<string>();

            foreach (ScoreDistribution distribution in Enum.GetValues(typeof(ScoreDistribution)))
            {
                var entries = LeaderboardGenerator.Generate(200, distribution, 20f, 300f, new Random(7));
                var data = LeaderboardDebuggerService.BuildData(1, entries);
                var problem = LeaderboardDebuggerService.CompareOrder(data.GetLevelLeaderBoard(1), entries);
                if (problem != null) problems.Add($"{distribution}: {problem}");
            }

            return Result(problems.FirstOrDefault(), "200 entries per distribution match the locally computed ascending order");
        }

        private static (bool, string) SortingSaved(int level)
        {
            var data = LeaderboardDebuggerService.Load();

            if (!data.Score.leaderBoardList.ContainsKey(level)) return (true, $"Level {level} has no saved entries, nothing to verify");

            var source = data.Score.leaderBoardList[level].Values.ToList();
            var problem = LeaderboardDebuggerService.CompareOrder(data.GetLevelLeaderBoard(level), source);
            return Result(problem, $"{source.Count} saved entries match the locally computed ascending order");
        }

        private static (bool, string) Ties()
        {
            var random = new Random(11);
            var entries = LeaderboardGenerator.Generate(20, ScoreDistribution.Uniform, 20f, 100f, random);
            entries.AddRange(LeaderboardGenerator.CreateTies(10, 50f, random));
            entries.AddRange(LeaderboardGenerator.CreateTies(5, 50f, random));
            var data = LeaderboardDebuggerService.BuildData(1, entries);

            var first = data.GetLevelLeaderBoard(1);
            var second = data.GetLevelLeaderBoard(1);

            var problem = FirstProblem(
                LeaderboardDebuggerService.CompareOrder(first, entries),
                first.Select(score => score.id).SequenceEqual(second.Select(score => score.id)) ? null : "Two reads of the same data returned tied entries in a different order");

            return Result(problem, "No entries lost and order is consistent between reads. The leaderboard has no tie-breaker, so the order among tied entries is not defined by any rule");
        }

        private static (bool, string) OneEntryPerId()
        {
            var data = new LeaderBoardData();
            LeaderboardDebuggerService.AddEntry(data, 1, "Name", "same-id", 30f);
            LeaderboardDebuggerService.AddEntry(data, 1, "Name", "same-id", 20f);
            var entries = data.GetLevelLeaderBoard(1);

            if (entries.Count != 1) return (false, $"Expected 1 entry for a repeated id, found {entries.Count}");
            return (true, "Writing the same id twice keeps a single entry");
        }

        private static (bool, string) OverwriteWithoutComparing()
        {
            var data = new LeaderBoardData();
            LeaderboardDebuggerService.AddEntry(data, 1, "Name", "same-id", 20f);
            LeaderboardDebuggerService.AddEntry(data, 1, "Name", "same-id", 30f);
            var stored = data.GetLevelLeaderBoard(1)[0].timescore;

            if (!stored.Equals(30f)) return (false, $"Expected the worse time 30 to overwrite 20 when written directly, stored {LeaderboardDebuggerService.FormatTime(stored)}");
            return (true, "A worse time overwrites a better one when AddTimeToLevel is called directly. The personal-best guard lives in the caller (SaveData.CheckBestTime and the NewRecord check in LevelWonPresenter)");
        }

        private static (bool, string) CheckBestTimeGuard()
        {
            var save = new SaveData();

            save.CheckBestTime(1, 20f, out var prev, out var best, out var newRecord);
            if (!newRecord || !best.Equals(20f)) return (false, "A first time on a level should count as a new record");

            save.SetNewBestTime(1, 20f);

            save.CheckBestTime(1, 25f, out prev, out best, out newRecord);
            if (newRecord || !best.Equals(20f) || !prev.Equals(20f)) return (false, "A worse time should not be a new record and the best should stay 20");

            save.CheckBestTime(1, 15f, out prev, out best, out newRecord);
            if (!newRecord || !best.Equals(15f)) return (false, "A better time should be a new record");

            save.CheckBestTime(1, 20f, out prev, out best, out newRecord);
            if (newRecord) return (false, "An equal time should not be a new record");

            return (true, "First time, worse, better and equal times are all classified correctly");
        }

        private static (bool, string) LevelsIndependent()
        {
            var data = new LeaderBoardData();
            LeaderboardDebuggerService.AddEntry(data, 1, "A", "id-a", 10f);
            LeaderboardDebuggerService.AddEntry(data, 2, "A", "id-a", 99f);
            LeaderboardDebuggerService.AddEntry(data, 2, "B", "id-b", 5f);

            var levelOne = data.GetLevelLeaderBoard(1);
            var levelTwo = data.GetLevelLeaderBoard(2);

            if (levelOne.Count != 1 || levelTwo.Count != 2) return (false, $"Expected 1 and 2 entries, found {levelOne.Count} and {levelTwo.Count}");
            if (!levelOne[0].timescore.Equals(10f) || !levelTwo[0].timescore.Equals(5f)) return (false, "Entries leaked between levels");
            return (true, "The same player id keeps a separate entry on each level");
        }

        private static List<Score> ExtremeEntries()
        {
            return LeaderboardGenerator.EdgeTimes.Select((time, index) => new Score { name = $"Edge{index}", id = $"edge-{index}", timescore = time }).ToList();
        }

        private static (bool, string) ExtremeTimesSort()
        {
            var entries = ExtremeEntries();
            var data = LeaderboardDebuggerService.BuildData(1, entries);
            var problem = LeaderboardDebuggerService.CompareOrder(data.GetLevelLeaderBoard(1), entries);
            return Result(problem, $"{entries.Count} extreme times (0, negative, MaxValue, Epsilon, infinities, NaN, precision values) sort as expected");
        }

        private static (bool, string) ExtremeTimesRoundTrip()
        {
            var data = LeaderboardDebuggerService.BuildData(1, ExtremeEntries());
            LeaderboardDebuggerService.Save(data);
            var problem = LeaderboardDebuggerService.Compare(data, LeaderboardDebuggerService.Load());
            return Result(problem, "Extreme times are identical after save and reload");
        }

        private static (bool, string) UnusualNamesRoundTrip()
        {
            var entries = LeaderboardGenerator.EdgeNames.Append(new string('A', 10000)).Select((text, index) => new Score { name = text, id = text, timescore = index }).ToList();
            var data = LeaderboardDebuggerService.BuildData(1, entries);
            LeaderboardDebuggerService.Save(data);
            var problem = LeaderboardDebuggerService.Compare(data, LeaderboardDebuggerService.Load());
            return Result(problem, $"{entries.Count} unusual names and ids (empty, unicode, emoji, quotes, slashes, control characters, 10000 characters) survive save and reload");
        }

        private static (bool, string) EmptyRoundTrip()
        {
            var data = new LeaderBoardData();
            LeaderboardDebuggerService.Save(data);
            var problem = LeaderboardDebuggerService.Compare(data, LeaderboardDebuggerService.Load());
            return Result(problem, "An empty leaderboard survives save and reload");
        }

        private static (bool, string) MissingLevelLookup()
        {
            var data = new LeaderBoardData();

            try
            {
                var entries = data.GetLevelLeaderBoard(999);
                return (true, $"Returned {entries.Count} entries");
            }
            catch (KeyNotFoundException)
            {
                return (false, "GetLevelLeaderBoard throws KeyNotFoundException for a level with no entries. LeaderboardPresenter.OnrecievedScore calls it without checking, so opening the leaderboard on a level nobody has finished would throw");
            }
        }

        private static (bool, string) SingleEntry()
        {
            var entries = new List<Score> { new Score { name = "Solo", id = "solo", timescore = 42.5f } };
            var data = LeaderboardDebuggerService.BuildData(1, entries);
            LeaderboardDebuggerService.Save(data);
            var reloaded = LeaderboardDebuggerService.Load();

            var problem = FirstProblem(
                LeaderboardDebuggerService.CompareOrder(reloaded.GetLevelLeaderBoard(1), entries),
                LeaderboardDebuggerService.Compare(data, reloaded));

            return Result(problem, "A single entry sorts and reloads correctly");
        }

        private static LeaderBoardData SampleData()
        {
            var data = new LeaderBoardData();
            var random = new Random(3);

            for (var level = 1; level <= 3; level++)
            {
                foreach (var entry in LeaderboardGenerator.Generate(100, ScoreDistribution.Normal, 20f, 200f, random))
                {
                    LeaderboardDebuggerService.AddEntry(data, level, entry.name, entry.id, entry.timescore);
                }
            }

            return data;
        }

        private static (bool, string) SaveReload()
        {
            var data = SampleData();
            LeaderboardDebuggerService.Save(data);
            var problem = LeaderboardDebuggerService.Compare(data, LeaderboardDebuggerService.Load());
            return Result(problem, "300 entries over 3 levels are identical after save and reload");
        }

        private static (bool, string) MissingFile()
        {
            LeaderboardDebuggerService.DeleteSave();
            var data = LeaderboardDebuggerService.Load();

            if (data == null) return (false, "Load returned null");
            if (data.Score.leaderBoardList.Count != 0) return (false, "Expected an empty default leaderboard");
            if (!LeaderboardDebuggerService.SaveExists) return (false, "Load did not create a default save file");
            return (true, "A missing file produces an empty default and writes it to disk");
        }

        private static (bool, string) CorruptedFile()
        {
            LeaderboardDebuggerService.WriteGarbage();
            var data = LeaderboardDebuggerService.Load();

            if (data == null) return (false, "Load returned null");
            if (data.Score.leaderBoardList.Count != 0) return (false, "Expected the corrupted file to reset to an empty leaderboard");
            return (true, "Garbage bytes reset the leaderboard to an empty default");
        }

        private static (bool, string) TruncatedFile()
        {
            LeaderboardDebuggerService.Save(SampleData());
            LeaderboardDebuggerService.Truncate();
            var data = LeaderboardDebuggerService.Load();

            if (data == null) return (false, "Load returned null");
            if (data.Score.leaderBoardList.Count != 0) return (false, "Expected the truncated file to reset to an empty leaderboard");
            return (true, "A half-written file resets the leaderboard to an empty default");
        }

        private static (bool, string) EditedFile()
        {
            var original = SampleData();
            LeaderboardDebuggerService.Save(original);
            LeaderboardDebuggerService.FlipBytes();
            var data = LeaderboardDebuggerService.Load();

            if (data == null) return (false, "Load returned null");

            var differences = LeaderboardDebuggerService.Compare(original, data);
            return (true, differences == null ? "The edited file still loaded identical data" : $"The edited file loaded without throwing. Result differs from the original: {differences}");
        }
    }
}
