using LPE;
using NUnit.Framework;
using UnityEngine;

namespace GridWorld.Tests {
    public class MovementSystemTests {
        TestWorld test;

        [SetUp]
        public void SetUp() {
            test = new TestWorld();
        }

        [TearDown]
        public void TearDown() {
            test.Dispose();
        }

        // Requests a step every tick the entity is allowed to, like a held direction.
        void HoldDirection(EntityID id, Direction4 direction, int ticks) {
            for (int i = 0; i < ticks; i++) {
                if (test.StepOf(id).state != StepState.Moving) {
                    test.Request(id, direction);
                }
                test.Tick();
            }
        }

        [Test]
        public void Tick_WalkRequest_AdvancesOnStartTickAndFinishesAfterWalkTicks() {
            var id = test.Spawn(Vector2Int.zero, Direction4.right);

            test.Request(id, Direction4.right);
            test.Tick();

            Assert.AreEqual(StepState.Moving, test.StepOf(id).state);
            Assert.AreEqual(1, test.StepOf(id).elapsed);
            Assert.AreEqual(16, test.StepOf(id).duration);

            test.Tick(14);
            Assert.AreEqual(StepState.Moving, test.StepOf(id).state);

            test.Tick();
            Assert.AreEqual(StepState.Finished, test.StepOf(id).state);
        }

        [Test]
        public void Tick_RunRequest_UsesRunTicks() {
            var id = test.Spawn(Vector2Int.zero, Direction4.right);

            test.Request(id, Direction4.right, run: true);
            test.Tick();

            Assert.AreEqual(8, test.StepOf(id).duration);
        }

        [Test]
        public void Tick_StepStarts_ClaimsTargetAndReleasesOrigin() {
            var id = test.Spawn(Vector2Int.zero, Direction4.right);

            test.Request(id, Direction4.right);
            test.Tick();

            Assert.AreEqual(new Vector2Int(1, 0), test.TileOf(id));
            Assert.AreEqual(id, test.world.occupancy.Get(new Vector2Int(1, 0)));
            Assert.IsFalse(test.world.occupancy.Get(Vector2Int.zero).IsValid());
            Assert.AreEqual(Vector2Int.zero, test.StepOf(id).from);
        }

        [Test]
        public void Tick_RequestsHeldAcrossSteps_KeepConstantRate() {
            var id = test.Spawn(Vector2Int.zero, Direction4.right);

            HoldDirection(id, Direction4.right, 16);
            Assert.AreEqual(new Vector2Int(1, 0), test.TileOf(id));
            Assert.AreEqual(StepState.Finished, test.StepOf(id).state);

            // The next step is requested and starts on the tick after the old one finishes.
            HoldDirection(id, Direction4.right, 1);
            Assert.AreEqual(new Vector2Int(2, 0), test.TileOf(id));
            Assert.AreEqual(1, test.StepOf(id).elapsed);

            HoldDirection(id, Direction4.right, 31);
            Assert.AreEqual(new Vector2Int(3, 0), test.TileOf(id));
            Assert.AreEqual(StepState.Finished, test.StepOf(id).state);
        }

        [Test]
        public void Tick_FinishedWithNoRequest_ClearsToIdleNextTick() {
            var id = test.Spawn(Vector2Int.zero, Direction4.right);
            test.Request(id, Direction4.right);
            test.Tick(16);

            test.Tick();

            Assert.AreEqual(StepState.Idle, test.StepOf(id).state);
            Assert.AreEqual(new Vector2Int(1, 0), test.StepOf(id).from);
        }

        [Test]
        public void Tick_RequestWhileMoving_IsIgnored() {
            var id = test.Spawn(Vector2Int.zero, Direction4.right);
            test.Request(id, Direction4.right);
            test.Tick();

            test.Request(id, Direction4.up);
            test.Tick();

            Assert.AreEqual(new Vector2Int(1, 0), test.TileOf(id));
            Assert.AreSame(Direction4.right, test.FacingOf(id));
            Assert.AreEqual(2, test.StepOf(id).elapsed);
        }

        [Test]
        public void Tick_TurnOnlyRequest_TurnsWithoutMoving() {
            var id = test.Spawn(Vector2Int.zero, Direction4.down);

            test.Request(id, Direction4.left, turnOnly: true);
            test.Tick();

            Assert.AreSame(Direction4.left, test.FacingOf(id));
            Assert.AreEqual(Vector2Int.zero, test.TileOf(id));
            Assert.AreEqual(StepState.Idle, test.StepOf(id).state);
        }

        [Test]
        public void Tick_TargetLacksEntryBit_BlocksStepButTurns() {
            var id = test.Spawn(Vector2Int.zero, Direction4.up);
            test.world.tiles.SetChange(new Vector2Int(1, 0), TileBits.EnterAll & ~TileBits.EnterRight);

            test.Request(id, Direction4.right);
            test.Tick();

            Assert.AreEqual(Vector2Int.zero, test.TileOf(id));
            Assert.AreSame(Direction4.right, test.FacingOf(id));
            Assert.AreEqual(StepState.Idle, test.StepOf(id).state);
        }

        [Test]
        public void Tick_TargetLacksOtherEntryBit_AllowsStep() {
            var id = test.Spawn(new Vector2Int(1, -1), Direction4.up);
            test.world.tiles.SetChange(new Vector2Int(1, 0), TileBits.EnterAll & ~TileBits.EnterRight);

            test.Request(id, Direction4.up);
            test.Tick();

            Assert.AreEqual(new Vector2Int(1, 0), test.TileOf(id));
        }

        [Test]
        public void Tick_TargetOutsideAreas_IsBlocked() {
            var edge = new Vector2Int(TestWorld.AreaBounds.xMax - 1, 0);
            var id = test.Spawn(edge, Direction4.right);

            test.Request(id, Direction4.right);
            test.Tick();

            Assert.AreEqual(edge, test.TileOf(id));
        }

        [Test]
        public void Tick_TargetOccupied_IsBlocked() {
            var id = test.Spawn(Vector2Int.zero, Direction4.right);
            var other = test.Spawn(new Vector2Int(1, 0), Direction4.down);

            test.Request(id, Direction4.right);
            test.Tick();

            Assert.AreEqual(Vector2Int.zero, test.TileOf(id));
            Assert.AreEqual(other, test.world.occupancy.Get(new Vector2Int(1, 0)));
        }

        [Test]
        public void Tick_PlayerAndNpcWantSameTile_PlayerWins() {
            var npc = test.Spawn(Vector2Int.zero, Direction4.right);
            var player = test.SpawnPlayer(new Vector2Int(1, 1), Direction4.down);

            test.Request(npc, Direction4.right);
            test.Request(player, Direction4.down);
            test.Tick();

            Assert.AreEqual(new Vector2Int(1, 0), test.TileOf(player));
            Assert.AreEqual(Vector2Int.zero, test.TileOf(npc));
        }

        [Test]
        public void Tick_TwoNpcsWantSameTile_LowestYWins() {
            var low = test.Spawn(Vector2Int.zero, Direction4.right);
            var high = test.Spawn(new Vector2Int(1, 1), Direction4.down);

            test.Request(high, Direction4.down);
            test.Request(low, Direction4.right);
            test.Tick();

            Assert.AreEqual(new Vector2Int(1, 0), test.TileOf(low));
            Assert.AreEqual(new Vector2Int(1, 1), test.TileOf(high));
        }

        [Test]
        public void Tick_TwoNpcsSameRowWantSameTile_LowestXWins() {
            var left = test.Spawn(Vector2Int.zero, Direction4.right);
            var right = test.Spawn(new Vector2Int(2, 0), Direction4.left);

            test.Request(right, Direction4.left);
            test.Request(left, Direction4.right);
            test.Tick();

            Assert.AreEqual(new Vector2Int(1, 0), test.TileOf(left));
            Assert.AreEqual(new Vector2Int(2, 0), test.TileOf(right));
        }

        [Test]
        public void Tick_FollowerResolvedBeforeLeader_RetriesNextTick() {
            // Moving right, the follower has the lower x, so it's resolved first and finds the leader still there.
            var follower = test.Spawn(Vector2Int.zero, Direction4.right);
            var leader = test.Spawn(new Vector2Int(1, 0), Direction4.right);

            test.Request(follower, Direction4.right);
            test.Request(leader, Direction4.right);
            test.Tick();

            Assert.AreEqual(new Vector2Int(2, 0), test.TileOf(leader));
            Assert.AreEqual(Vector2Int.zero, test.TileOf(follower));

            test.Request(follower, Direction4.right);
            test.Tick();

            Assert.AreEqual(new Vector2Int(1, 0), test.TileOf(follower));
        }

        [Test]
        public void Tick_FollowerResolvedAfterLeader_MovesSameTick() {
            // Moving left, the leader has the lower x, so it frees its tile before the follower is resolved.
            var leader = test.Spawn(Vector2Int.zero, Direction4.left);
            var follower = test.Spawn(new Vector2Int(1, 0), Direction4.left);

            test.Request(follower, Direction4.left);
            test.Request(leader, Direction4.left);
            test.Tick();

            Assert.AreEqual(new Vector2Int(-1, 0), test.TileOf(leader));
            Assert.AreEqual(Vector2Int.zero, test.TileOf(follower));
        }

        [Test]
        public void Tick_AdjacentEntitiesSwap_BothBlocked() {
            var a = test.Spawn(Vector2Int.zero, Direction4.right);
            var b = test.Spawn(new Vector2Int(1, 0), Direction4.left);

            test.Request(a, Direction4.right);
            test.Request(b, Direction4.left);
            test.Tick();

            Assert.AreEqual(Vector2Int.zero, test.TileOf(a));
            Assert.AreEqual(new Vector2Int(1, 0), test.TileOf(b));
        }
    }
}
