using System;
using System.Collections.Generic;
using System.IO;
using _Main.Scripts.Ghost;
using UnityEditor;
using UnityEngine;

namespace Game.GhostDebugger
{
    public enum GhostSource
    {
        Scene,
        File
    }

    /// <summary>
    /// Scene ghost, active run, timeline and settings shared by every tab of the ghost debugger window.
    /// </summary>
    [Serializable]
    public sealed class GhostDebuggerContext
    {
        private const string ShowHelpKey = "GhostDebugger.ShowHelp";
        private const string SandboxKey = "GhostDebugger.UseSandbox";
        private const string WindowOpenKey = "GhostDebugger.WindowOpen";
        private const string ExportFolderKey = "GhostDebugger.ExportFolder";

        [SerializeField] private GhostSource source = GhostSource.Scene;
        [SerializeField] private string filePath = "";
        [SerializeField] private bool showHelp = true;
        [SerializeField] private bool useSandbox = true;
        [SerializeField] private string exportFolder = "";
        [SerializeField] private float speed = 1f;
        [SerializeField] private bool showPath = true;
        [SerializeField] private bool showMarkers;
        [SerializeField] private bool speedColor = true;
        [SerializeField] private int pathWindow = 50;
        [SerializeField] private float fidelityTolerance = 0.001f;
        [SerializeField] private float rotationTolerance = 0.1f;
        [SerializeField] private LayerMask wallMask;
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private float probeHeight = 1f;
        [SerializeField] private float probeRadius = 0.2f;
        [SerializeField] private float groundDistance = 5f;

        private bool sceneDirty = true;
        private GhostRecorder.GhostRunData fileRun;
        private GhostRecorder.GhostRunData builtFrom;
        private bool resumeOnRelease;
        private double anchorReal;
        private float anchorTime;

        public GhostSource Source => source;
        public string FilePath => filePath;
        public GhostPlayer Player { get; private set; }
        public GhostRecorder Recorder { get; private set; }
        public GhostRecorder.GhostRunData Run { get; private set; }
        public string RunError { get; private set; }
        public List<string> RunIssues { get; private set; } = new List<string>();
        public List<GhostPlaybackUtils.Frame> Frames { get; } = new List<GhostPlaybackUtils.Frame>();
        public float MaxSpeed { get; private set; }
        public float Time { get; private set; }
        public bool Controlling { get; private set; }
        public bool Playing { get; private set; }
        public int Revision { get; private set; }

        public bool HasRun => Frames.Count >= 2;
        public bool HasSceneGhost => Player && Player.GhostTransform;
        public bool OwnsTime => source == GhostSource.File || Controlling || !Player;
        public float Duration => Frames.Count == 0 ? 0f : GhostPlaybackUtils.Duration(Frames, Run.durationSeconds);
        public string RunName => source == GhostSource.File ? Path.GetFileName(filePath) : Player ? "Scene ghost" : "None";
        public float Speed => speed;
        public string ExportFolder => string.IsNullOrEmpty(exportFolder) ? GhostDebuggerService.DefaultExportFolder : exportFolder;

        public bool ShowPath { get => showPath; set => showPath = value; }
        public bool ShowMarkers { get => showMarkers; set => showMarkers = value; }
        public bool SpeedColor { get => speedColor; set => speedColor = value; }
        public int PathWindow { get => pathWindow; set => pathWindow = Mathf.Max(1, value); }
        public float FidelityTolerance { get => fidelityTolerance; set => fidelityTolerance = Mathf.Max(0f, value); }
        public float RotationTolerance { get => rotationTolerance; set => rotationTolerance = Mathf.Max(0f, value); }
        public LayerMask WallMask { get => wallMask; set => wallMask = value; }
        public LayerMask GroundMask { get => groundMask; set => groundMask = value; }
        public float ProbeHeight { get => probeHeight; set => probeHeight = value; }
        public float ProbeRadius { get => probeRadius; set => probeRadius = Mathf.Max(0.01f, value); }
        public float GroundDistance { get => groundDistance; set => groundDistance = Mathf.Max(0.1f, value); }

        public bool ShowHelp
        {
            get => showHelp;
            set
            {
                if (showHelp == value) return;

                showHelp = value;
                EditorPrefs.SetBool(ShowHelpKey, value);
            }
        }

        public bool UseSandbox
        {
            get => useSandbox;
            set
            {
                if (useSandbox == value) return;

                useSandbox = value;
                EditorPrefs.SetBool(SandboxKey, value);
                ApplySandbox();
            }
        }

        [InitializeOnLoadMethod]
        private static void ApplySandboxOnLoad()
        {
            if (EditorPrefs.GetBool(WindowOpenKey, false) && EditorPrefs.GetBool(SandboxKey, true))
            {
                GhostRecorder.FolderOverride = GhostDebuggerService.SandboxFolder;
            }
        }

        public void LoadPreferences()
        {
            showHelp = EditorPrefs.GetBool(ShowHelpKey, true);
            useSandbox = EditorPrefs.GetBool(SandboxKey, true);
            exportFolder = EditorPrefs.GetString(ExportFolderKey, "");
            if (wallMask.value == 0) wallMask = LayerMask.GetMask("Wall", "Obstacle");
            if (groundMask.value == 0) groundMask = LayerMask.GetMask("Ground");
        }

        public void WindowOpened()
        {
            EditorPrefs.SetBool(WindowOpenKey, true);
            ApplySandbox();
        }

        public void WindowClosed()
        {
            EditorPrefs.SetBool(WindowOpenKey, false);
            if (Player) Player.ExternalTimeControl = false;
            GhostRecorder.FolderOverride = null;
        }

        public void ApplySandbox()
        {
            GhostRecorder.FolderOverride = useSandbox ? GhostDebuggerService.SandboxFolder : null;
        }

        public void SetExportFolder(string folder)
        {
            exportFolder = folder;
            EditorPrefs.SetString(ExportFolderKey, folder);
        }

        public void MarkSceneDirty()
        {
            sceneDirty = true;
        }

        public void OnPlayModeChanged()
        {
            Controlling = false;
            Playing = false;
            Player = null;
            Recorder = null;
            sceneDirty = true;
            ApplySandbox();
        }

        public bool Poll()
        {
            var changed = false;

            if (sceneDirty || (!Player && !ReferenceEquals(Player, null)))
            {
                sceneDirty = false;
                if (!Player) Player = UnityEngine.Object.FindObjectOfType<GhostPlayer>();
                if (!Recorder) Recorder = UnityEngine.Object.FindObjectOfType<GhostRecorder>();
                changed = true;
            }

            if (Controlling && !Player) Controlling = false;

            var run = source == GhostSource.File ? fileRun : Player ? Player.LoadedRun : null;
            if (!ReferenceEquals(run, builtFrom))
            {
                Rebuild(run);
                changed = true;
            }

            if (!OwnsTime && Player && !Mathf.Approximately(Time, Player.PlaybackTime))
            {
                Time = Player.PlaybackTime;
                changed = true;
            }

            return changed;
        }

        public bool Tick()
        {
            if (!Playing || !OwnsTime || !HasRun) return false;

            var t = anchorTime + (float)(EditorApplication.timeSinceStartup - anchorReal) * speed;
            if (t >= Duration)
            {
                t = Duration;
                Playing = false;
            }

            ApplyTime(t);
            return true;
        }

        public void SelectScene()
        {
            source = GhostSource.Scene;
            fileRun = null;
            RunError = null;
            Playing = false;
            sceneDirty = true;
        }

        public void SelectFile(string path)
        {
            source = GhostSource.File;
            filePath = path;
            Playing = false;
            RunError = null;
            fileRun = GhostDebuggerService.TryRead(path, out var data, out var error) ? data : null;
            if (fileRun == null) RunError = error;
            Rebuild(fileRun);
        }

        public void UseRun(GhostRecorder.GhostRunData data, string path)
        {
            if (Player)
            {
                Player.LoadRun(GhostDebuggerService.Clone(data), path);
                source = GhostSource.Scene;
                fileRun = null;
                RunError = null;
                Rebuild(Player.LoadedRun);
                SetTime(0f);
                return;
            }

            SelectFile(path);
        }

        public void TakeControl(bool control)
        {
            if (!Player) control = false;
            if (Controlling == control) return;

            Controlling = control;
            Playing = false;
            if (!Player) return;

            Player.ExternalTimeControl = control;
            if (control)
            {
                resumeOnRelease = Player.IsPlaying;
                Player.Pause();
                Time = Player.PlaybackTime;
            }
            else if (resumeOnRelease)
            {
                Player.Play();
            }
        }

        public void TogglePlay()
        {
            if (!OwnsTime)
            {
                if (Player.IsPlaying) Player.Pause();
                else Player.Play();
                return;
            }

            Playing = !Playing;
            if (Playing && Time >= Duration) ApplyTime(0f);
            Anchor();
        }

        public void SetSpeed(float value)
        {
            speed = Mathf.Max(0.01f, value);
            Anchor();
        }

        public void SetTime(float t)
        {
            ApplyTime(t);
            Anchor();
        }

        public void JumpTo(float t)
        {
            if (Player && source == GhostSource.Scene) TakeControl(true);
            Playing = false;
            SetTime(t);

            var view = SceneView.lastActiveSceneView;
            if (view && HasRun) view.LookAt(SamplePosition(Time));
        }

        public void StepSample(int delta)
        {
            if (!HasRun) return;

            Playing = false;
            var index = Mathf.Clamp(CurrentIndex() + delta, 0, Frames.Count - 1);
            SetTime(Frames[index].time);
        }

        public void Restart()
        {
            if (!OwnsTime && Player)
            {
                Player.Stop();
                Player.Play();
                return;
            }

            SetTime(0f);
        }

        public int CurrentIndex()
        {
            if (!HasRun) return 0;

            var i1 = GhostPlaybackUtils.AdvanceFrameIndex(Frames, Time, 1);
            return Time >= Frames[i1].time ? i1 : i1 - 1;
        }

        public void Bracket(float t, out int previous, out int next, out float percent)
        {
            next = GhostPlaybackUtils.AdvanceFrameIndex(Frames, t, 1);
            previous = next - 1;
            var span = Mathf.Max(1e-5f, Frames[next].time - Frames[previous].time);
            percent = Mathf.Clamp01((t - Frames[previous].time) / span);
        }

        public Vector3 SamplePosition(float t)
        {
            Bracket(t, out var previous, out var next, out _);
            return GhostPlaybackUtils.SamplePosition(Frames, previous, next, t, GhostPlaybackUtils.PosInterp.Linear);
        }

        public Quaternion SampleRotation(float t)
        {
            Bracket(t, out var previous, out var next, out _);
            return GhostPlaybackUtils.SampleRotation(Frames, previous, next, t, GhostPlaybackUtils.RotMode.UseRecordedRotation, Vector3.zero, Vector3.zero, 0f, 0f);
        }

        public float SegmentSpeed(int index)
        {
            if (index <= 0 || index >= Frames.Count) return 0f;

            var span = Frames[index].time - Frames[index - 1].time;
            return span > 1e-5f ? Vector3.Distance(Frames[index].position, Frames[index - 1].position) / span : 0f;
        }

        private void ApplyTime(float t)
        {
            Time = Mathf.Clamp(t, 0f, Duration);
            if (Controlling && Player) Player.SetPlaybackTime(Time);
        }

        private void Anchor()
        {
            anchorTime = Time;
            anchorReal = EditorApplication.timeSinceStartup;
        }

        private void Rebuild(GhostRecorder.GhostRunData run)
        {
            builtFrom = run;
            Run = run;
            Frames.Clear();
            MaxSpeed = 0f;
            Revision++;
            RunIssues = GhostDebuggerService.Validate(run);

            if (run?.frames == null) return;

            foreach (var frame in run.frames)
            {
                Frames.Add(new GhostPlaybackUtils.Frame { time = frame.time, position = frame.position, rotation = frame.rotation });
            }

            for (var i = 1; i < Frames.Count; i++)
            {
                MaxSpeed = Mathf.Max(MaxSpeed, SegmentSpeed(i));
            }

            if (Time > Duration) Time = Duration;
        }
    }
}
