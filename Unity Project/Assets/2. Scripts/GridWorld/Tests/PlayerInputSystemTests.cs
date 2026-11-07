using LPE;
using NUnit.Framework;
using UnityEngine;

namespace GridWorld.Tests {
    public class PlayerInputSystemTests {
        TestWorld test;
        EntityID player;

        [SetUp]
        public void SetUp() {
            test = new TestWorld();
            player = test.SpawnPlayer(Vector2Int.zero, Direction4.down);
        }

        [TearDown]
        public void TearDown() {
            test.Dispose();
        }

        void Hold(Direction4 direction) {
            test.world.input.SetDirectionHeld(direction, true);
        }

        void Release(Direction4 direction) {
            test.world.input.SetDirectionHeld(direction, false);
        }

        [Test]
        public void Tick_TapDirectionNotFacing_TurnsOnly() {
            Hold(Direction4.right);
            test.Tick();
            Release(Direction4.right);
            test.Tick(20);

            Assert.AreSame(Direction4.right, test.FacingOf(player));
            Assert.AreEqual(Vector2Int.zero, test.TileOf(player));
        }

        [Test]
        public void Tick_HoldDirectionNotFacing_WalksAfterHoldTicks() {
            Hold(Direction4.right);

            test.Tick(PlayerInputSystem.TurnHoldTicks);
            Assert.AreSame(Direction4.right, test.FacingOf(player));
            Assert.AreEqual(Vector2Int.zero, test.TileOf(player));

            test.Tick();
            Assert.AreEqual(new Vector2Int(1, 0), test.TileOf(player));
            Assert.AreEqual(StepState.Moving, test.StepOf(player).state);
        }

        [Test]
        public void Tick_HoldDirectionAlreadyFacing_WalksImmediately() {
            Hold(Direction4.down);

            test.Tick();

            Assert.AreEqual(new Vector2Int(0, -1), test.TileOf(player));
        }

        [Test]
        public void Tick_HoldDirection_WalksContinuouslyAtConstantRate() {
            Hold(Direction4.down);

            test.Tick(32);
            Assert.AreEqual(new Vector2Int(0, -2), test.TileOf(player));
            Assert.AreEqual(StepState.Finished, test.StepOf(player).state);

            test.Tick();
            Assert.AreEqual(new Vector2Int(0, -3), test.TileOf(player));
            Assert.AreEqual(1, test.StepOf(player).elapsed);
        }

        [Test]
        public void Tick_NewDirectionWhileWalking_NextStepStartsWithoutDelay() {
            Hold(Direction4.down);
            test.Tick(10);

            Hold(Direction4.right);
            test.Tick(7);

            Assert.AreEqual(new Vector2Int(1, -1), test.TileOf(player));
            Assert.AreSame(Direction4.right, test.FacingOf(player));
            Assert.AreEqual(StepState.Moving, test.StepOf(player).state);
        }

        [Test]
        public void Tick_RunPressedMidStep_AppliesFromNextStep() {
            Hold(Direction4.down);
            test.Tick(5);

            test.world.input.runHeld = true;
            test.Tick(11);
            Assert.AreEqual(16, test.StepOf(player).duration);

            test.Tick();
            Assert.AreEqual(8, test.StepOf(player).duration);
        }

        [Test]
        public void Tick_ReleasedMidStep_StopsWhenStepFinishes() {
            Hold(Direction4.down);
            test.Tick(5);

            Release(Direction4.down);
            test.Tick(20);

            Assert.AreEqual(new Vector2Int(0, -1), test.TileOf(player));
            Assert.AreEqual(StepState.Idle, test.StepOf(player).state);
        }

        [Test]
        public void Tick_HoldIntoWall_TurnsAndStaysPut() {
            test.world.tiles.SetChange(new Vector2Int(-1, 0), 0);

            Hold(Direction4.left);
            test.Tick(20);

            Assert.AreSame(Direction4.left, test.FacingOf(player));
            Assert.AreEqual(Vector2Int.zero, test.TileOf(player));
        }
    }
}
