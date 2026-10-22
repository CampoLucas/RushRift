using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Main.Scripts.Ghost;
using UnityEditor;
using UnityEngine;

namespace Game.GhostDebugger
{
    /// <summary>
    /// Outcome of one ghost check, with the time to jump to when it failed.
    /// </summary>
    public sealed class GhostCheckResult
    {
        public string Name { get; private set; }
        public bool Passed { get; private set; }
        public string Message { get; private set; }
        public float FailTime { get; private set; }

        public GhostCheckResult(string name, bool passed, string message, float failTime = -1f)
        {
            Name = name;
            Passed = passed;
            Message = message;
            FailTime = failTime;
        }
    }

    /// <summary>
    /// One check. Its enumerator yields null to wait an editor update and ends by yielding a GhostCheckResult.
    /// </summary>
    public sealed class GhostCheck
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public Func<GhostDebuggerContext, IEnumerator> Run { get; private set; }

        public GhostCheck(string name, string description, Func<GhostDebuggerContext, IEnumerator> run)
        {
            Name = name;
            Description = description;
            Run = run;
        }
    }

    /// <summary>
    /// Checks that run one scenario on the loaded ghost and report a measured value.
    /// </summary>
    public static class GhostChecks
    {
        private const int MaxListed = 15;

        public static readonly List<GhostCheck> All = new List<GhostCheck>
        {
            new GhostCheck("Fidelity sweep", "Puts the ghost on every sample and measures the largest distance from the recorded position.", Fidelity),
            new GhostCheck("Rotation sweep", "Puts the ghost on every sample and measures the largest angle from the recorded rotation.", Rotation),
            new GhostCheck("Time gaps", "Finds gaps between samples that are much shorter (duplicates) or longer (lag spikes, standing still) than the recording interval.", TimeGaps),
            new GhostCheck("Start", "Stops the scene ghost before playing and confirms it stays on sample 0 for 30 updates.", Start),
            new GhostCheck("Finish", "Checks the last sample is at the finish trigger and the run time in the file matches the last sample time.", Finish),
            new GhostCheck("Pause", "Pauses the game for 3 s during playback and confirms the ghost does not move and does not jump when unpaused.", Pause),
            new GhostCheck("Path", "Flags samples inside walls or with no ground below, using the layer masks set here.", Path)
        };

        private static IEnumerator Fidelity(GhostDebuggerContext context)
        {
            if (!context.HasRun)
            {
                yield return NoRun("Fidelity sweep");
                yield break;
            }

            var frames = context.Frames;
            var worst = 0f;
            var worstIndex = 0;
            var offset = Vector3.zero;
            var live = context.Source == GhostSource.Scene && context.HasSceneGhost;
            var wasControlling = context.Controlling;
            var wasPlaying = live && context.Player.IsPlaying;
            var restoreTime = context.Time;
            if (live) context.TakeControl(true);

            for (var i = 0; i < frames.Count; i++)
            {
                Vector3 position;
                if (live)
                {
                    context.Player.SetPlaybackTime(frames[i].time);
                    position = context.Player.GhostTransform.position;
                    if (i == 0) offset = position - frames[0].position;
                }
                else
                {
                    var i1 = GhostPlaybackUtils.AdvanceFrameIndex(frames, frames[i].time, 1);
                    position = GhostPlaybackUtils.SamplePosition(frames, i1 - 1, i1, frames[i].time, GhostPlaybackUtils.PosInterp.CatmullRom);
                }

                var distance = Vector3.Distance(position, frames[i].position + offset);
                if (distance > worst)
                {
                    worst = distance;
                    worstIndex = i;
                }
            }

            if (live)
            {
                context.SetTime(restoreTime);
                context.TakeControl(wasControlling);
                if (wasPlaying && !wasControlling) context.Player.Play();
            }

            var source = live ? $"scene ghost, start offset {offset.magnitude:0.000} m removed" : "math only, no scene ghost";
            var passed = worst <= context.FidelityTolerance;
            yield return new GhostCheckResult("Fidelity sweep", passed, $"Largest distance {worst:0.00000} m at sample {worstIndex} ({frames[worstIndex].time:0.000} s), tolerance {context.FidelityTolerance} ({source}).", passed ? -1f : frames[worstIndex].time);
        }

        private static IEnumerator Rotation(GhostDebuggerContext context)
        {
            if (!context.HasRun)
            {
                yield return NoRun("Rotation sweep");
                yield break;
            }

            var frames = context.Frames;
            var worst = 0f;
            var worstIndex = 0;
            var live = context.Source == GhostSource.Scene && context.HasSceneGhost;
            var wasControlling = context.Controlling;
            var wasPlaying = live && context.Player.IsPlaying;
            var restoreTime = context.Time;
            if (live) context.TakeControl(true);

            for (var i = 0; i < frames.Count; i++)
            {
                Quaternion rotation;
                if (live)
                {
                    context.Player.SetPlaybackTime(frames[i].time);
                    rotation = context.Player.GhostTransform.rotation;
                }
                else
                {
                    var i1 = GhostPlaybackUtils.AdvanceFrameIndex(frames, frames[i].time, 1);
                    rotation = GhostPlaybackUtils.SampleRotation(frames, i1 - 1, i1, frames[i].time, GhostPlaybackUtils.RotMode.UseRecordedRotation, Vector3.zero, Vector3.zero, 0f, 0f);
                }

                var angle = Quaternion.Angle(rotation, frames[i].rotation);
                if (angle > worst)
                {
                    worst = angle;
                    worstIndex = i;
                }
            }

            if (live)
            {
                context.SetTime(restoreTime);
                context.TakeControl(wasControlling);
                if (wasPlaying && !wasControlling) context.Player.Play();
            }

            var passed = worst <= context.RotationTolerance;
            yield return new GhostCheckResult("Rotation sweep", passed, $"Largest angle {worst:0.0000}° at sample {worstIndex} ({frames[worstIndex].time:0.000} s), tolerance {context.RotationTolerance}° ({(live ? "scene ghost" : "math only")}).", passed ? -1f : frames[worstIndex].time);
        }

        private static IEnumerator TimeGaps(GhostDebuggerContext context)
        {
            if (!context.HasRun)
            {
                yield return NoRun("Time gaps");
                yield break;
            }

            var frames = context.Frames;
            var interval = RecordInterval(context, out var intervalSource);
            var flagged = new List<string>();
            var firstTime = -1f;

            for (var i = 1; i < frames.Count; i++)
            {
                var gap = frames[i].time - frames[i - 1].time;
                string kind = null;
                if (gap <= 1e-6f) kind = "duplicate";
                else if (gap < interval * 0.5f) kind = "short";
                else if (gap > interval * 1.5f) kind = "long";

                if (kind == null) continue;

                if (firstTime < 0f) firstTime = frames[i].time;
                flagged.Add($"#{i} {kind} {gap:0.0000} s");
            }

            var listed = string.Join(", ", flagged.Take(MaxListed)) + (flagged.Count > MaxListed ? $" and {flagged.Count - MaxListed} more" : "");
            var message = flagged.Count == 0
                ? $"All {frames.Count - 1} gaps are within 0.5x to 1.5x of {interval:0.0000} s ({intervalSource})."
                : $"{flagged.Count} gaps outside 0.5x to 1.5x of {interval:0.0000} s ({intervalSource}): {listed}. Long gaps can also mean the player stood still, because the recorder skips frames without movement.";
            yield return new GhostCheckResult("Time gaps", flagged.Count == 0, message, firstTime);
        }

        private static IEnumerator Start(GhostDebuggerContext context)
        {
            if (!NeedsLiveGhost(context, "Start", out var problem))
            {
                yield return problem;
                yield break;
            }

            var player = context.Player;
            var wasPlaying = player.IsPlaying;
            context.TakeControl(false);
            player.Stop();
            var start = player.GhostTransform.position;
            var drift = 0f;

            for (var i = 0; i < 30; i++)
            {
                yield return null;
                drift = Mathf.Max(drift, Vector3.Distance(start, player.GhostTransform.position));
            }

            var fromSample = Vector3.Distance(start, context.Frames[0].position);
            if (wasPlaying) player.Play();

            var passed = drift <= context.FidelityTolerance;
            yield return new GhostCheckResult("Start", passed, $"Moved {drift:0.00000} m in 30 updates while stopped. Distance from sample 0: {fromSample:0.000} m (start alignment moves it up to 0.5 m).", passed ? -1f : 0f);
        }

        private static IEnumerator Finish(GhostDebuggerContext context)
        {
            if (!context.HasRun)
            {
                yield return NoRun("Finish");
                yield break;
            }

            var last = context.Frames[context.Frames.Count - 1];
            var timeDifference = Mathf.Abs(context.Run.durationSeconds - last.time);
            var timeOk = timeDifference <= 0.02f;
            var triggers = UnityEngine.Object.FindObjectsOfType<WinTrigger>();
            string lineMessage;
            var lineOk = true;

            if (triggers.Length == 0)
            {
                lineMessage = "No WinTrigger in the open scenes, so the finish line position was not checked.";
            }
            else
            {
                var closest = float.MaxValue;
                foreach (var trigger in triggers)
                {
                    var collider = trigger.GetComponent<Collider>();
                    var point = collider ? collider.ClosestPoint(last.position) : trigger.transform.position;
                    closest = Mathf.Min(closest, Vector3.Distance(point, last.position));
                }

                lineOk = closest <= 0.5f;
                lineMessage = $"Last sample is {closest:0.000} m from the nearest finish trigger (limit 0.5 m).";
            }

            yield return new GhostCheckResult("Finish", timeOk && lineOk, $"Run time in file {context.Run.durationSeconds:0.000} s, last sample {last.time:0.000} s, difference {timeDifference:0.000} s (limit 0.02 s). {lineMessage}", timeOk && lineOk ? -1f : last.time);
        }

        private static IEnumerator Pause(GhostDebuggerContext context)
        {
            if (!NeedsLiveGhost(context, "Pause", out var problem))
            {
                yield return problem;
                yield break;
            }

            var player = context.Player;
            var wasPaused = PauseHandler.IsPaused;
            context.TakeControl(false);
            PauseHandler.Pause(false);
            player.Play();

            var warmUp = EditorApplication.timeSinceStartup + 0.3;
            while (EditorApplication.timeSinceStartup < warmUp) yield return null;

            PauseHandler.Pause(true);
            var pausedAt = player.GhostTransform.position;
            var pausedTime = player.PlaybackTime;
            var moved = 0f;
            var until = EditorApplication.timeSinceStartup + 3.0;
            while (EditorApplication.timeSinceStartup < until)
            {
                yield return null;
                moved = Mathf.Max(moved, Vector3.Distance(pausedAt, player.GhostTransform.position));
            }

            var resumedAt = EditorApplication.timeSinceStartup;
            PauseHandler.Pause(false);
            yield return null;
            var elapsed = (float)(EditorApplication.timeSinceStartup - resumedAt);
            var jump = Vector3.Distance(pausedAt, player.GhostTransform.position);
            var allowed = context.MaxSpeed * Mathf.Max(elapsed, 0.05f) * 2f + context.FidelityTolerance;
            PauseHandler.Pause(wasPaused);

            var passed = moved <= context.FidelityTolerance && jump <= allowed;
            yield return new GhostCheckResult("Pause", passed, $"Moved {moved:0.00000} m while paused for 3 s. Moved {jump:0.000} m in the first update after unpause (allowed {allowed:0.000} m at top speed).", passed ? -1f : pausedTime);
        }

        private static IEnumerator Path(GhostDebuggerContext context)
        {
            if (!context.HasRun)
            {
                yield return NoRun("Path");
                yield break;
            }

            if (context.WallMask.value == 0 && context.GroundMask.value == 0)
            {
                yield return new GhostCheckResult("Path", false, "Set a wall mask or a ground mask first.");
                yield break;
            }

            var frames = context.Frames;
            var flagged = new List<string>();
            var firstTime = -1f;
            var up = Vector3.up * context.ProbeHeight;

            for (var i = 0; i < frames.Count; i++)
            {
                var probe = frames[i].position + up;
                string kind = null;
                if (context.WallMask.value != 0 && Physics.CheckSphere(probe, context.ProbeRadius, context.WallMask, QueryTriggerInteraction.Ignore)) kind = "in wall";
                else if (context.GroundMask.value != 0 && !Physics.Raycast(probe, Vector3.down, context.ProbeHeight + context.GroundDistance, context.GroundMask, QueryTriggerInteraction.Ignore)) kind = "no ground";

                if (kind == null) continue;

                if (firstTime < 0f) firstTime = frames[i].time;
                flagged.Add($"#{i} {kind}");
            }

            var listed = string.Join(", ", flagged.Take(MaxListed)) + (flagged.Count > MaxListed ? $" and {flagged.Count - MaxListed} more" : "");
            yield return new GhostCheckResult("Path", flagged.Count == 0, flagged.Count == 0 ? $"All {frames.Count} samples are clear of walls and above ground." : $"{flagged.Count} samples flagged: {listed}. Needs the level scene open so its colliders exist.", firstTime);
        }

        private static float RecordInterval(GhostDebuggerContext context, out string source)
        {
            if (context.Recorder)
            {
                source = "GhostRecorder interval";
                return Mathf.Max(1e-4f, new SerializedObject(context.Recorder).FindProperty("minFrameIntervalSeconds").floatValue);
            }

            var gaps = new List<float>();
            for (var i = 1; i < context.Frames.Count; i++) gaps.Add(context.Frames[i].time - context.Frames[i - 1].time);
            gaps.Sort();
            source = "median gap, no GhostRecorder in the scene";
            return Mathf.Max(1e-4f, gaps[gaps.Count / 2]);
        }

        private static bool NeedsLiveGhost(GhostDebuggerContext context, string name, out GhostCheckResult problem)
        {
            problem = null;
            if (!Application.isPlaying) problem = new GhostCheckResult(name, false, "Needs Play Mode.");
            else if (context.Source != GhostSource.Scene || !context.HasSceneGhost) problem = new GhostCheckResult(name, false, "Needs the scene ghost selected in the Ghost dropdown.");
            else if (!context.HasRun) problem = new GhostCheckResult(name, false, "The scene ghost has no run loaded.");
            return problem == null;
        }

        private static GhostCheckResult NoRun(string name)
        {
            return new GhostCheckResult(name, false, "No ghost run is loaded.");
        }
    }
}
