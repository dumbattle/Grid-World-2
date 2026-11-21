using System.Collections.Generic;
using LPE;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GridWorld.Tests {
    public class AITests {
        class CountingAI : EntityAI {
            public int calls;

            public override void Decide(AIContext ctx, EntityID self) {
                calls++;
            }
        }

        TestWorld test;

        [SetUp]
        public void SetUp() {
            test = new TestWorld();
            test.SetAreaActive(true);
        }

        [TearDown]
        public void TearDown() {
            test.Dispose();
        }

        EntityID SpawnWith(EntityAI ai, Vector2Int tile) {
            var id = test.Spawn(tile, Direction4.down);
            test.AddAI(id, ai);
            return id;
        }

        // Ticks until done, and lists each patrol point the entity reaches, in order.
        List<Vector2Int> PointsReached(EntityID id, Vector2Int[] points, int ticks) {
            var reached = new List<Vector2Int>();
            if (System.Array.IndexOf(points, test.TileOf(id)) >= 0) {
                reached.Add(test.TileOf(id));
            }
            for (int i = 0; i < ticks; i++) {
                test.Tick();
                var tile = test.TileOf(id);
                if (test.StepOf(id).state == StepState.Moving) {
                    continue;
                }
                if (System.Array.IndexOf(points, tile) >= 0 && (reached.Count == 0 || reached[reached.Count - 1] != tile)) {
                    reached.Add(tile);
                }
            }
            return reached;
        }

        static PatrolPoint[] Route(params Vector2Int[] tiles) {
            var points = new PatrolPoint[tiles.Length];
            for (int i = 0; i < tiles.Length; i++) {
                points[i] = new PatrolPoint(tiles[i], null, 0);
            }
            return points;
        }

        [Test]
        public void Run_EntityMidStep_SkipsAI() {
            var ai = new CountingAI();
            var id = SpawnWith(ai, Vector2Int.zero);

            test.Request(id, Direction4.right);
            test.Tick();
            test.Tick(14);

            Assert.AreEqual(1, ai.calls);
            Assert.AreEqual(StepState.Moving, test.StepOf(id).state);
        }

        [Test]
        public void Run_AreaInactive_PausesUnlessTheAIOptsOut() {
            var paused = new CountingAI();
            var running = new CountingAI { pausesWhenInactive = false };
            SpawnWith(paused, Vector2Int.zero);
            SpawnWith(running, Vector2Int.one);
            test.SetAreaActive(false);

            test.Tick(3);

            Assert.AreEqual(0, paused.calls);
            Assert.AreEqual(3, running.calls);
        }

        [Test]
        public void SpinAI_AfterWait_TurnsInPlace() {
            var id = SpawnWith(new SpinAI(0, 0), Vector2Int.zero);

            test.Tick(2);

            Assert.AreNotEqual(Direction4.down, test.FacingOf(id));
            Assert.AreEqual(Vector2Int.zero, test.TileOf(id));
        }

        [Test]
        public void WanderAI_ManyTicks_MovesButStaysInRegion() {
            var region = new RectInt(-1, -1, 3, 3);
            var id = SpawnWith(new WanderAI(region, 0, 10), Vector2Int.zero);
            bool moved = false;

            for (int i = 0; i < 2000; i++) {
                test.Tick();
                Assert.IsTrue(region.Contains(test.TileOf(id)), $"Left the region at {test.TileOf(id)}.");
                moved |= test.TileOf(id) != Vector2Int.zero;
            }
            Assert.IsTrue(moved);
        }

        [Test]
        public void WanderAI_NoFreeTileInRegion_StaysPut() {
            var id = SpawnWith(new WanderAI(new RectInt(0, 0, 1, 1), 0, 0), Vector2Int.zero);

            test.Tick(200);

            Assert.AreEqual(Vector2Int.zero, test.TileOf(id));
        }

        [Test]
        public void PatrolAI_Loop_VisitsPointsInOrderAndReturns() {
            var tiles = new[] { new Vector2Int(0, 0), new Vector2Int(2, 0), new Vector2Int(2, 2), new Vector2Int(0, 2) };
            var id = SpawnWith(new PatrolAI(Route(tiles), PatrolMode.Loop), tiles[0]);

            var reached = PointsReached(id, tiles, 8 * 16 + 20);

            CollectionAssert.AreEqual(new[] { tiles[0], tiles[1], tiles[2], tiles[3], tiles[0] }, reached);
        }

        [Test]
        public void PatrolAI_PingPong_WalksBackTheWayItCame() {
            var tiles = new[] { new Vector2Int(0, 0), new Vector2Int(2, 0), new Vector2Int(2, 2) };
            var id = SpawnWith(new PatrolAI(Route(tiles), PatrolMode.PingPong), tiles[0]);

            var reached = PointsReached(id, tiles, 8 * 16 + 20);

            CollectionAssert.AreEqual(new[] { tiles[0], tiles[1], tiles[2], tiles[1], tiles[0] }, reached);
        }

        [Test]
        public void PatrolAI_PointWithFacingAndWait_TurnsThenWaits() {
            var points = new[] {
                new PatrolPoint(new Vector2Int(0, 0), null, 0),
                new PatrolPoint(new Vector2Int(1, 0), Direction4.up, 30),
            };
            var id = SpawnWith(new PatrolAI(points, PatrolMode.Loop), Vector2Int.zero);

            // One tick to start the step, 16 to walk it, then one to turn.
            test.Tick(1 + 16 + 1);
            Assert.AreEqual(new Vector2Int(1, 0), test.TileOf(id));
            Assert.AreEqual(Direction4.up, test.FacingOf(id));

            test.Tick(25);
            Assert.AreEqual(new Vector2Int(1, 0), test.TileOf(id));
            Assert.AreEqual(StepState.Idle, test.StepOf(id).state);

            test.Tick(10);
            Assert.AreEqual(Direction4.left, test.FacingOf(id));
        }

        [Test]
        public void PatrolAI_Blocked_WaitsThenCarriesOn() {
            var tiles = new[] { new Vector2Int(0, 0), new Vector2Int(2, 0) };
            var id = SpawnWith(new PatrolAI(Route(tiles), PatrolMode.PingPong), tiles[0]);
            var blocker = test.Spawn(new Vector2Int(1, 0), Direction4.down);

            test.Tick(40);
            Assert.AreEqual(tiles[0], test.TileOf(id));
            Assert.AreEqual(Direction4.right, test.FacingOf(id));

            // The patrol moves first in the tie-break, so it's blocked once more while the blocker
            // leaves. It then claims (2,0) when its second step starts, 17 ticks after its first.
            test.Request(blocker, Direction4.up);
            test.Tick(1 + 17 + 5);

            Assert.AreEqual(tiles[1], test.TileOf(id));
        }

        [Test]
        public void PatrolAI_OutOfLine_StandsStillAndWarnsOnce() {
            var tiles = new[] { new Vector2Int(0, 0), new Vector2Int(2, 0) };
            var id = SpawnWith(new PatrolAI(Route(tiles), PatrolMode.Loop), new Vector2Int(1, 1));

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("out of line"));
            test.Tick(30);

            Assert.AreEqual(new Vector2Int(1, 1), test.TileOf(id));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void FindRouteError_DiagonalClosingLeg_FailsOnlyForLoop() {
            var tiles = new[] { new Vector2Int(0, 0), new Vector2Int(2, 0), new Vector2Int(2, 2) };

            Assert.IsNotNull(PatrolAI.FindRouteError(tiles, PatrolMode.Loop));
            Assert.IsNull(PatrolAI.FindRouteError(tiles, PatrolMode.PingPong));
        }

        [Test]
        public void FindRouteError_OnePoint_Fails() {
            Assert.IsNotNull(PatrolAI.FindRouteError(new[] { Vector2Int.zero }, PatrolMode.PingPong));
        }
    }
}
