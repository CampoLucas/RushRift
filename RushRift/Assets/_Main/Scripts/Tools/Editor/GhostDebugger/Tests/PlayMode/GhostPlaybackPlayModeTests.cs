using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.GhostDebugger.Tests
{
    /// <summary>
    /// Ghost playback in an empty scene made in code.
    /// </summary>
    public class GhostPlaybackPlayModeTests
    {
        private GhostTestAdapter _adapter;
        private GhostTestTempFolder _folder;
        private Scene _scene;
        private SimulationMode _simulationMode;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _simulationMode = Physics.simulationMode;
            _scene = SceneManager.CreateScene("GhostTestScene");
            SceneManager.SetActiveScene(_scene);
            _adapter = new GhostTestAdapter();
            _folder = new GhostTestTempFolder();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _adapter.Dispose();
            _folder.Dispose();
            Physics.simulationMode = _simulationMode;
            Time.timeScale = 1f;
            yield return SceneManager.UnloadSceneAsync(_scene);
        }

        [UnityTest]
        [Description("Spawns a ghost from a 20 m straight run, starts the race and checks it is at 10 m after 1 s.")]
        public IEnumerator Ghost_SpawnsFromSavedRun_AndMoves()
        {
            _adapter.SetTime(0f);
            var ghost = _adapter.SpawnGhost(GhostTestRuns.StraightLine());
            Assert.IsNotNull(ghost, "no ghost was spawned");
            var spawnPosition = ghost.transform.position;

            _adapter.StartRace();
            _adapter.SetTime(1f);
            yield return null;

            Assert.Greater(Vector3.Distance(spawnPosition, ghost.transform.position), 1f, "ghost did not move");
            GhostTestAssert.Near(new Vector3(10f, 0f, 0f), ghost.transform.position, GhostTestAssert.PositionTolerance, "t = 1.0");
        }

        [UnityTest]
        [Description("Spawns a ghost without starting the race and checks it stays on the first sample for 30 frames.")]
        public IEnumerator Ghost_BeforeRaceStarts_StaysOnFirstSample()
        {
            _adapter.SetTime(0f);
            var ghost = _adapter.SpawnGhost(GhostTestRuns.StraightLine());

            for (var frame = 0; frame < 30; frame++)
            {
                _adapter.SetTime(frame / 29f);
                yield return null;

                GhostTestAssert.Near(Vector3.zero, ghost.transform.position, GhostTestAssert.PositionTolerance, $"frame {frame}");
            }
        }

        [UnityTest]
        [Description("Pauses the game halfway, waits 2 s, and checks the ghost never moves while paused and continues from the same spot after.")]
        public IEnumerator Ghost_WhenPaused_HoldsPosition_AndResumesWithoutJump()
        {
            _adapter.SetTime(0f);
            var ghost = _adapter.SpawnGhost(GhostTestRuns.StraightLine());
            _adapter.StartRace();
            _adapter.SetTime(0.5f);
            yield return null;
            GhostTestAssert.Near(new Vector3(5f, 0f, 0f), ghost.transform.position, GhostTestAssert.PositionTolerance, "before pause");

            _adapter.PauseGame();
            for (var k = 1; k <= 100; k++)
            {
                _adapter.SetTime(0.5f + k * GhostTestClock.Step);
                yield return null;

                GhostTestAssert.Near(new Vector3(5f, 0f, 0f), ghost.transform.position, GhostTestAssert.PositionTolerance, $"paused step {k}");
            }

            _adapter.ResumeGame();
            _adapter.SetTime(2.6f);
            yield return null;

            GhostTestAssert.Near(new Vector3(6f, 0f, 0f), ghost.transform.position, GhostTestAssert.PositionTolerance, "after resume");
        }

        [UnityTest]
        [Description("Moves the ghost through finish, checkpoint and player probes with physics stepped by hand and checks it touches none of them.")]
        public IEnumerator Ghost_NeverTriggersFinishCheckpointsOrPlayer()
        {
            Physics.simulationMode = SimulationMode.Script;
            _adapter.SetTime(0f);
            var ghost = _adapter.SpawnGhost(GhostTestRuns.StraightLine());
            var finish = CreateProbe("Finish", _adapter.FinishLayer, true, ghost.transform);
            var checkpoint = CreateProbe("Checkpoint", _adapter.CheckpointLayer, true, ghost.transform);
            var player = CreateProbe("Player", _adapter.PlayerLayer, false, ghost.transform);
            _adapter.StartRace();

            for (var step = 0; step <= 100; step++)
            {
                _adapter.SetTime(GhostTestClock.TimeAt(step));
                yield return null;
                Physics.Simulate(GhostTestClock.Step);
            }

            Assert.AreEqual(0, finish.Contacts, "ghost touched the finish line");
            Assert.AreEqual(0, checkpoint.Contacts, "ghost touched the checkpoint");
            Assert.AreEqual(0, player.Contacts, "ghost touched the player");
        }

        [UnityTest]
        [Description("Exports a run to JSON, imports it, plays it and checks the ghost reaches the finish at 2.0 s (within one frame).")]
        public IEnumerator Ghost_FromExportedJson_ReachesFinishOnTime()
        {
            var path = _folder.File("run.json");
            _adapter.ExportJson(GhostTestRuns.StraightLine(), path);
            var imported = _adapter.ImportJson(path);

            _adapter.SetTime(0f);
            var ghost = _adapter.SpawnGhost(imported);
            _adapter.StartRace();

            var reachedAt = -1f;
            for (var frame = 0; frame <= 150 && reachedAt < 0f; frame++)
            {
                var time = frame / 60f;
                _adapter.SetTime(time);
                yield return null;

                if (Vector3.Distance(ghost.transform.position, new Vector3(20f, 0f, 0f)) <= GhostTestAssert.PositionTolerance)
                {
                    reachedAt = time;
                }
            }

            Assert.GreaterOrEqual(reachedAt, 0f, "ghost never reached the finish");
            Assert.AreEqual(2f, reachedAt, 1f / 60f, "finish time");
        }

        private static GhostTestContactProbe CreateProbe(string name, int layer, bool isTrigger, Transform ghost)
        {
            var probe = new GameObject(name)
            {
                layer = layer
            };
            probe.transform.position = new Vector3(10f, 0f, 0f);

            var collider = probe.AddComponent<BoxCollider>();
            collider.size = new Vector3(4f, 4f, 4f);
            collider.isTrigger = isTrigger;

            var body = probe.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = isTrigger;
            body.constraints = RigidbodyConstraints.FreezeAll;

            var counter = probe.AddComponent<GhostTestContactProbe>();
            counter.Ghost = ghost;
            return counter;
        }
    }
}
