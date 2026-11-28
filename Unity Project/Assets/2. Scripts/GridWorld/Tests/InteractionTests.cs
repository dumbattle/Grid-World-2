using System;
using System.Text.RegularExpressions;
using LPE;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GridWorld.Tests {
    public class InteractionTests {
        TestWorld test;
        EntityID player;

        [SetUp]
        public void SetUp() {
            test = new TestWorld();
            player = test.SpawnPlayer(Vector2Int.zero, Direction4.right);
        }

        [TearDown]
        public void TearDown() {
            test.Dispose();
        }

        int attemptCount => test.world.interactAttempts.Count;

        // Ticks, failing if any tick records an attempt.
        void TickWithNoAttempts(int ticks) {
            for (int i = 0; i < ticks; i++) {
                test.Tick();
                Assert.AreEqual(0, attemptCount, $"An attempt was recorded on tick {i + 1} of {ticks}.");
            }
        }

        // Counts its builds, and holds its entity, if any, for a few ticks.
        class CountingCutscene : ICutscene {
            readonly EntityID holds;
            public int builds;

            public CountingCutscene() {
                holds = EntityID.INVALID;
            }

            public CountingCutscene(EntityID holds) {
                this.holds = holds;
            }

            public void Build(CutsceneBuildContext ctx, CutsceneBuilder b) {
                builds++;
                if (holds.IsValid()) {
                    b.Lock(holds);
                }
                b.Wait(10);
            }
        }

        // --- Attempts ---

        [Test]
        public void Tick_IdlePressFacingEntity_RecordsAttemptWithTarget() {
            var npc = test.Spawn(Vector2Int.right, Direction4.down);
            test.PressInteract();

            test.Tick();

            Assert.AreEqual(1, attemptCount);
            var attempt = test.world.interactAttempts[0];
            Assert.AreEqual(player, attempt.entity);
            Assert.AreEqual(Vector2Int.right, attempt.tile);
            Assert.AreEqual(npc, attempt.target);
        }

        [Test]
        public void Tick_PressFacingEmptyTile_RecordsAttemptWithNoTarget() {
            test.PressInteract();

            test.Tick();

            Assert.AreEqual(1, attemptCount);
            Assert.IsFalse(test.world.interactAttempts[0].target.IsValid());
        }

        [Test]
        public void Tick_PressWithDirectionHeld_InteractsInsteadOfMoving() {
            test.world.input.SetDirectionHeld(Direction4.right, true);
            test.PressInteract();

            test.Tick();

            Assert.AreEqual(1, attemptCount);
            Assert.AreEqual(StepState.Idle, test.StepOf(player).state);
            Assert.AreEqual(Vector2Int.zero, test.TileOf(player));
        }

        [Test]
        public void Tick_AfterAttempt_ClearsItNextTick() {
            test.PressInteract();
            test.Tick();

            test.Tick();

            Assert.AreEqual(0, attemptCount);
        }

        [Test]
        public void Tick_PressDuringWalk_FiresOnceIdleWithinBuffer() {
            // The step runs ticks 1 to 16, and the player is idle again in tick 18's decision phase.
            test.Request(player, Direction4.right);
            test.Tick(12);
            test.PressInteract();

            TickWithNoAttempts(5);
            test.Tick();

            Assert.AreEqual(1, attemptCount);
        }

        [Test]
        public void Tick_PressOlderThanBuffer_IsDropped() {
            test.Request(player, Direction4.right);
            test.Tick(11);
            test.PressInteract();

            TickWithNoAttempts(20);
        }

        [Test]
        public void Tick_PressWhileLocked_IsDropped() {
            test.StartCutscene(new LambdaCutscene((ctx, b) => {
                b.Lock(player);
                b.Wait(3);
            }));
            test.Tick();
            test.PressInteract();

            // Without the drop, the press would still be buffered when the cutscene ends.
            TickWithNoAttempts(8);
            Assert.IsFalse(test.IsLocked(player));
        }

        // --- The lookup ---

        [Test]
        public void TryGetInteractionAt_EntityOnInteractTile_EntityComesFirst() {
            var tile = new Vector2Int(3, 3);
            var forTile = new CountingCutscene();
            var forEntity = new CountingCutscene();
            test.AddTileInteraction(tile, forTile);
            test.AddEntityInteraction(test.Spawn(tile, Direction4.down), forEntity);

            Assert.IsTrue(test.TryGetInteractionAt(tile, out var found));
            Assert.AreSame(forEntity, found);
        }

        [Test]
        public void TryGetInteractionAt_EntityWalksAway_InteractionFollowsIt() {
            var npc = test.Spawn(new Vector2Int(3, 3), Direction4.right);
            var cutscene = new CountingCutscene();
            test.AddEntityInteraction(npc, cutscene);

            test.Request(npc, Direction4.right);
            test.Tick();

            Assert.IsFalse(test.TryGetInteractionAt(new Vector2Int(3, 3), out _));
            Assert.IsTrue(test.TryGetInteractionAt(new Vector2Int(4, 3), out var found));
            Assert.AreSame(cutscene, found);
        }

        [Test]
        public void TryGetInteractionAt_EntityWithoutInteractionOnInteractTile_FindsTheTiles() {
            var tile = new Vector2Int(3, 3);
            var forTile = new CountingCutscene();
            test.AddTileInteraction(tile, forTile);
            test.Spawn(tile, Direction4.down);

            Assert.IsTrue(test.TryGetInteractionAt(tile, out var found));
            Assert.AreSame(forTile, found);
        }

        [Test]
        public void RemoveTileInteraction_Added_IsGone() {
            test.AddTileInteraction(Vector2Int.one, new CountingCutscene());

            test.RemoveTileInteraction(Vector2Int.one);

            Assert.IsFalse(test.TryGetInteractionAt(Vector2Int.one, out _));
        }

        [Test]
        public void RemoveEntityInteraction_Added_IsGone() {
            var npc = test.Spawn(Vector2Int.one, Direction4.down);
            test.AddEntityInteraction(npc, new CountingCutscene());

            test.RemoveEntityInteraction(npc);

            Assert.IsFalse(test.TryGetInteractionAt(Vector2Int.one, out _));
        }

        [Test]
        public void AddTileInteraction_TileAlreadyHasOne_Throws() {
            test.AddTileInteraction(Vector2Int.one, new CountingCutscene());

            Assert.Throws<InvalidOperationException>(() => test.AddTileInteraction(Vector2Int.one, new CountingCutscene()));
        }

        [Test]
        public void AddEntityInteraction_EntityAlreadyHasOne_Throws() {
            var npc = test.Spawn(Vector2Int.one, Direction4.down);
            test.AddEntityInteraction(npc, new CountingCutscene());

            Assert.Throws<InvalidOperationException>(() => test.AddEntityInteraction(npc, new CountingCutscene()));
        }

        // --- Starting the cutscene ---

        [Test]
        public void Interact_FacingEntityWithInteraction_StartsItNextTick() {
            var npc = test.Spawn(Vector2Int.right, Direction4.down);
            var cutscene = new CountingCutscene(npc);
            test.AddEntityInteraction(npc, cutscene);
            test.PressInteract();

            test.Tick(2);

            Assert.AreEqual(1, cutscene.builds);
            Assert.IsTrue(test.IsLocked(npc));
        }

        [Test]
        public void Interact_FacingInteractTile_StartsItsCutscene() {
            var cutscene = new CountingCutscene();
            test.AddTileInteraction(Vector2Int.right, cutscene);
            test.PressInteract();

            test.Tick(2);

            Assert.AreEqual(1, cutscene.builds);
            Assert.AreEqual(1, test.world.runningCutscenes.Count);
        }

        [Test]
        public void Interact_FacingNothing_StartsNothing() {
            test.Spawn(Vector2Int.right, Direction4.down);
            test.PressInteract();

            test.Tick(2);

            Assert.AreEqual(0, test.world.runningCutscenes.Count);
            Assert.AreEqual(0, test.world.cutsceneRequests.Count);
        }

        [Test]
        public void Interact_TargetLocked_DropsTheCutscene() {
            var npc = test.Spawn(Vector2Int.right, Direction4.down);
            test.AddEntityInteraction(npc, new CountingCutscene(npc));
            var busy = new LambdaCutscene((ctx, b) => {
                b.Lock(npc);
                b.Wait(20);
            });
            test.StartCutscene(busy);
            test.PressInteract();
            test.Tick();

            LogAssert.Expect(LogType.Log, new Regex($"'{nameof(CountingCutscene)}' didn't start"));
            test.Tick();

            Assert.AreEqual(1, test.world.runningCutscenes.Count);
            Assert.AreSame(busy, test.world.runningCutscenes[0].cutscene);
        }

        [Test]
        public void Interact_TargetDespawnedAfterAttempt_StartsNothing() {
            var npc = test.Spawn(Vector2Int.right, Direction4.down);
            var cutscene = new CountingCutscene();
            test.AddEntityInteraction(npc, cutscene);
            test.PressInteract();
            test.Tick();

            test.Despawn(npc);
            test.Tick();

            Assert.AreEqual(0, cutscene.builds);
            Assert.AreEqual(0, test.world.components.interaction.count);
        }

        // --- Feature IDs ---

        [Test]
        public void TryGetEntity_IdInOtherCase_FindsTheEntity() {
            var npc = test.Spawn(Vector2Int.one, Direction4.down);
            test.SetFeatureId(npc, "TEST_Greeter");

            Assert.IsTrue(test.TryGetEntity("test_greeter", out var found));
            Assert.AreEqual(npc, found);
        }

        [Test]
        public void TryGetEntity_AfterDespawn_FindsNothing() {
            var npc = test.Spawn(Vector2Int.one, Direction4.down);
            test.SetFeatureId(npc, "TEST_Greeter");

            test.Despawn(npc);

            Assert.IsFalse(test.TryGetEntity("TEST_Greeter", out _));
        }

        [Test]
        public void SetFeatureId_IdTakenIgnoringCase_Throws() {
            test.SetFeatureId(test.Spawn(Vector2Int.one, Direction4.down), "TEST_Greeter");
            var other = test.Spawn(new Vector2Int(2, 2), Direction4.down);

            Assert.Throws<InvalidOperationException>(() => test.SetFeatureId(other, "test_GREETER"));
        }
    }
}
