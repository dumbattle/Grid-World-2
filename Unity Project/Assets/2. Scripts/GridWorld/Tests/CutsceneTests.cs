using System;
using System.Text.RegularExpressions;
using LPE;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GridWorld.Tests {
    public class CutsceneTests {
        // Logs name a cutscene by its type.
        const string Name = nameof(LambdaCutscene);

        TestWorld test;

        class CountingAI : EntityAI {
            public int decisions;

            public CountingAI() {
                pausesWhenInactive = false;
            }

            public override void Decide(AIContext ctx, EntityID self) {
                decisions++;
            }
        }

        [SetUp]
        public void SetUp() {
            test = new TestWorld();
        }

        [TearDown]
        public void TearDown() {
            test.Dispose();
        }

        LambdaCutscene Play(Action<CutsceneBuildContext, CutsceneBuilder> build) {
            var cutscene = new LambdaCutscene(build);
            test.StartCutscene(cutscene);
            return cutscene;
        }

        // --- Starting and locks ---

        [Test]
        public void Start_Cutscene_LocksItsEntitiesUntilItEnds() {
            var a = test.Spawn(Vector2Int.zero, Direction4.right);
            var b = test.Spawn(new Vector2Int(0, 2), Direction4.right);
            var other = test.Spawn(new Vector2Int(0, 4), Direction4.right);
            Play((ctx, builder) => {
                builder.Face(a, Direction4.up);
                builder.Lock(b);
                builder.Wait(5);
            });

            test.Tick();
            Assert.IsTrue(test.IsLocked(a));
            Assert.IsTrue(test.IsLocked(b));
            Assert.IsFalse(test.IsLocked(other));

            test.Tick(5);
            Assert.IsFalse(test.IsLocked(a));
            Assert.IsFalse(test.IsLocked(b));
            Assert.AreEqual(0, test.world.runningCutscenes.Count);
        }

        [Test]
        public void Start_EntityAlreadyLocked_DropsTheSecondCutscene() {
            var a = test.Spawn(Vector2Int.zero, Direction4.right);
            var first = Play((ctx, builder) => builder.Walk(a, Direction4.right, 3));
            test.Tick();

            Play((ctx, builder) => builder.Face(a, Direction4.down));
            LogAssert.Expect(LogType.Log, new Regex($"'{Name}' didn't start"));
            test.Tick();

            Assert.AreEqual(1, test.world.runningCutscenes.Count);
            Assert.AreSame(first, test.world.runningCutscenes[0].cutscene);
        }

        [Test]
        public void StartCutscene_Null_Throws() {
            Assert.Throws<ArgumentNullException>(() => test.StartCutscene(null));
        }

        [Test]
        public void Start_SameInstanceTwice_BuildsEachTimeFromCurrentState() {
            var a = test.Spawn(Vector2Int.zero, Direction4.right);
            var builtFrom = new System.Collections.Generic.List<Vector2Int>();
            var cutscene = Play((ctx, builder) => {
                builtFrom.Add(ctx.GetTile(a));
                builder.Walk(a, Direction4.right, 1);
            });
            test.Tick(17);

            test.StartCutscene(cutscene);
            test.Tick(17);

            CollectionAssert.AreEqual(new[] { Vector2Int.zero, Vector2Int.right }, builtFrom);
            Assert.AreEqual(new Vector2Int(2, 0), test.TileOf(a));
        }

        // --- Walk ---

        [Test]
        public void Walk_TwoTiles_KeepsTheWalkRateAndEndsWhenTheLastStepFinishes() {
            var a = test.Spawn(Vector2Int.zero, Direction4.right);
            Play((ctx, builder) => builder.Walk(a, Direction4.right, 2));

            test.Tick();
            Assert.AreEqual(StepState.Moving, test.StepOf(a).state);
            Assert.AreEqual(new Vector2Int(1, 0), test.TileOf(a));

            test.Tick(15);
            Assert.AreEqual(StepState.Finished, test.StepOf(a).state);

            // Tick 17: the second step starts with no gap.
            test.Tick();
            Assert.AreEqual(StepState.Moving, test.StepOf(a).state);
            Assert.AreEqual(new Vector2Int(2, 0), test.TileOf(a));

            test.Tick(15);
            Assert.AreEqual(StepState.Finished, test.StepOf(a).state);
            Assert.IsTrue(test.IsLocked(a));

            // Tick 33: the walk is done, and the cutscene with it.
            test.Tick();
            Assert.IsFalse(test.IsLocked(a));
            Assert.AreEqual(new Vector2Int(2, 0), test.TileOf(a));
        }

        [Test]
        public void Walk_TwoWalksInARow_SecondStartsOnTheTickTheFirstEnds() {
            var a = test.Spawn(Vector2Int.zero, Direction4.right);
            Play((ctx, builder) => {
                builder.Walk(a, Direction4.right, 1);
                builder.Walk(a, Direction4.up, 1);
            });

            test.Tick(17);

            Assert.AreEqual(StepState.Moving, test.StepOf(a).state);
            Assert.AreEqual(new Vector2Int(1, 1), test.TileOf(a));
            Assert.AreEqual(1, test.StepOf(a).elapsed);
        }

        [Test]
        public void Walk_EntityMidStep_CountsFromTheStepsTarget() {
            var a = test.Spawn(Vector2Int.zero, Direction4.right);
            test.Request(a, Direction4.right);
            test.Tick(4);
            Play((ctx, builder) => builder.Walk(a, Direction4.right, 1));

            // The first step finishes on tick 16, and the walk's step starts on tick 17.
            test.Tick(12);
            Assert.AreEqual(new Vector2Int(1, 0), test.TileOf(a));
            test.Tick();
            Assert.AreEqual(new Vector2Int(2, 0), test.TileOf(a));
            Assert.AreEqual(StepState.Moving, test.StepOf(a).state);
        }

        [Test]
        public void Walk_BlockedForTheTimeout_TeleportsToItsDestination() {
            var a = test.Spawn(Vector2Int.zero, Direction4.right);
            test.Spawn(new Vector2Int(1, 0), Direction4.left);
            Play((ctx, builder) => builder.Walk(a, Direction4.right, 2));

            test.Tick(WalkStepsSequence.TimeoutTicks);
            Assert.AreEqual(Vector2Int.zero, test.TileOf(a));

            LogAssert.Expect(LogType.Warning, new Regex("teleported to"));
            test.Tick();

            Assert.AreEqual(new Vector2Int(2, 0), test.TileOf(a));
            Assert.AreEqual(StepState.Idle, test.StepOf(a).state);
            Assert.AreEqual(a, test.world.occupancy.Get(new Vector2Int(2, 0)));
            Assert.IsFalse(test.world.occupancy.Get(Vector2Int.zero).IsValid());
            Assert.IsFalse(test.IsLocked(a));
        }

        [Test]
        public void Walk_TimeoutWithDestinationTaken_LandsOnTheNearestFreeTile() {
            var a = test.Spawn(Vector2Int.zero, Direction4.right);
            test.Spawn(new Vector2Int(1, 0), Direction4.left);
            test.Spawn(new Vector2Int(2, 0), Direction4.left);
            Play((ctx, builder) => builder.Walk(a, Direction4.right, 2));

            LogAssert.Expect(LogType.Warning, new Regex("teleported to"));
            test.Tick(WalkStepsSequence.TimeoutTicks + 1);

            Assert.AreEqual(new Vector2Int(2, -1), test.TileOf(a));
        }

        // --- Other steps ---

        [Test]
        public void Face_Entity_TurnsWithoutMoving() {
            var a = test.Spawn(Vector2Int.zero, Direction4.right);
            Play((ctx, builder) => builder.Face(a, Direction4.up));

            test.Tick();

            Assert.AreEqual(Direction4.up, test.FacingOf(a));
            Assert.AreEqual(Vector2Int.zero, test.TileOf(a));
            Assert.AreEqual(StepState.Idle, test.StepOf(a).state);
        }

        [Test]
        public void Wait_TenTicks_NextStepStartsTenTicksLater() {
            var a = test.Spawn(Vector2Int.zero, Direction4.right);
            Play((ctx, builder) => {
                builder.Face(a, Direction4.up);
                builder.Wait(10);
                builder.Walk(a, Direction4.up, 1);
            });

            // The face runs on tick 1, so the walk starts on tick 11.
            test.Tick(10);
            Assert.AreEqual(StepState.Idle, test.StepOf(a).state);

            test.Tick();
            Assert.AreEqual(StepState.Moving, test.StepOf(a).state);
        }

        [Test]
        public void Parallel_TwoWalks_BothMoveAtOnce() {
            var a = test.Spawn(Vector2Int.zero, Direction4.right);
            var b = test.Spawn(new Vector2Int(0, 2), Direction4.right);
            var c = test.Spawn(new Vector2Int(0, 4), Direction4.right);
            Play((ctx, builder) => {
                builder.Parallel(
                    x => x.Walk(a, Direction4.right, 1),
                    x => x.Walk(b, Direction4.right, 2),
                    x => x.Walk(c, Direction4.right, 1));
                builder.Face(a, Direction4.down);
            });

            test.Tick();
            Assert.AreEqual(StepState.Moving, test.StepOf(a).state);
            Assert.AreEqual(StepState.Moving, test.StepOf(b).state);
            Assert.AreEqual(StepState.Moving, test.StepOf(c).state);

            // The face waits for the longest branch, whose last step finishes on tick 32, so the
            // walk ends on tick 33.
            test.Tick(31);
            Assert.AreEqual(Direction4.right, test.FacingOf(a));
            test.Tick();
            Assert.AreEqual(Direction4.down, test.FacingOf(a));
        }

        [Test]
        public void Extend_AfterAWalk_ReadsTheNewTile() {
            var a = test.Spawn(Vector2Int.zero, Direction4.right);
            var seen = new Vector2Int(-1, -1);
            Play((ctx, builder) => {
                builder.Walk(a, Direction4.right, 2);
                builder.Extend((now, e) => {
                    seen = now.GetTile(a);
                    e.Face(a, Direction4.down);
                });
            });

            test.Tick(33);

            Assert.AreEqual(new Vector2Int(2, 0), seen);
            Assert.AreEqual(Direction4.down, test.FacingOf(a));
        }

        [Test]
        public void Extend_UnlockedEntity_StopsTheCutsceneWithAnError() {
            var a = test.Spawn(Vector2Int.zero, Direction4.right);
            var other = test.Spawn(new Vector2Int(0, 2), Direction4.right);
            Play((ctx, builder) => {
                builder.Face(a, Direction4.up);
                builder.Extend((now, e) => e.Face(other, Direction4.up));
            });

            LogAssert.Expect(LogType.Error, new Regex($"'{Name}' stopped after an error"));
            test.Tick();

            Assert.AreEqual(0, test.world.runningCutscenes.Count);
            Assert.IsFalse(test.IsLocked(a));
            Assert.AreEqual(Direction4.right, test.FacingOf(other));
        }

        // --- Decision systems ---

        [Test]
        public void AISystem_LockedEntity_DoesntDecide() {
            var a = test.Spawn(Vector2Int.zero, Direction4.right);
            var ai = new CountingAI();
            test.AddAI(a, ai);
            Play((ctx, builder) => {
                builder.Lock(a);
                builder.Wait(5);
            });

            test.Tick(5);
            Assert.AreEqual(0, ai.decisions);

            // Tick 6 ends the cutscene before the decision phase.
            test.Tick();
            Assert.AreEqual(1, ai.decisions);
        }

        [Test]
        public void PlayerInput_LockedPlayer_IgnoresInputUntilTheCutsceneEnds() {
            var player = test.SpawnPlayer(Vector2Int.zero, Direction4.right);
            test.world.input.SetDirectionHeld(Direction4.right, true);
            Play((ctx, builder) => {
                builder.Lock(player);
                builder.Wait(5);
            });

            test.Tick(5);
            Assert.AreEqual(StepState.Idle, test.StepOf(player).state);

            test.Tick();
            Assert.AreEqual(StepState.Moving, test.StepOf(player).state);
        }

        // --- Despawning ---

        [Test]
        public void Walk_EntityDespawned_EndsTheStepAndTheCutsceneCarriesOn() {
            var a = test.Spawn(Vector2Int.zero, Direction4.right);
            var other = test.Spawn(new Vector2Int(0, 2), Direction4.right);
            Play((ctx, builder) => {
                builder.Walk(a, Direction4.right, 3);
                builder.Face(other, Direction4.up);
            });
            test.Tick(5);

            test.Despawn(a);
            LogAssert.Expect(LogType.Warning, new Regex("despawned"));
            test.Tick();

            Assert.AreEqual(Direction4.up, test.FacingOf(other));
            Assert.AreEqual(0, test.world.runningCutscenes.Count);
            Assert.IsFalse(test.IsLocked(other));
        }
    }
}
