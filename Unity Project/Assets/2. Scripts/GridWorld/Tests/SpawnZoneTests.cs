using System;
using System.Collections.Generic;
using LPE;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace GridWorld.Tests {
    public class SpawnZoneTests {
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

        SpawnZone Zone(RectInt region, int maxCount = 3, int delay = 10, SpawnAIData ai = null) {
            return test.AddSpawnZone(new SpawnZone(region, test.definition, maxCount, delay, ai));
        }

        // A zone that always spawns on one tile, to prove the steps can be overridden.
        class FixedTileZone : SpawnZone {
            Vector2Int tile;

            public FixedTileZone(RectInt region, EntityDefinitionSO definition, Vector2Int tile) : base(region, definition, 1, 0, null) {
                this.tile = tile;
            }

            protected override bool TryChooseTile(SpawnContext ctx, out Vector2Int chosen) {
                chosen = tile;
                return true;
            }
        }

        [Test]
        public void Tick_ActiveArea_SpawnsAMobInTheRegion() {
            var region = new RectInt(0, 0, 3, 3);
            var zone = Zone(region);

            test.Tick();

            Assert.AreEqual(1, zone.mobs.Count);
            Assert.IsTrue(region.Contains(test.TileOf(zone.mobs[0])));
        }

        [Test]
        public void Tick_InactiveArea_SpawnsNothing() {
            test.SetAreaActive(false);
            var zone = Zone(new RectInt(0, 0, 3, 3));

            test.Tick(50);

            Assert.AreEqual(0, zone.mobs.Count);
        }

        [Test]
        public void Tick_InactiveArea_DelayDoesntCountDown() {
            var zone = Zone(new RectInt(0, 0, 3, 3), delay: 10);
            test.Tick();
            test.SetAreaActive(false);

            test.Tick(20);

            Assert.AreEqual(10, zone.ticksUntilSpawn);
        }

        [Test]
        public void Tick_RespawnDelay_SpawnsOneMobPerDelay() {
            var zone = Zone(new RectInt(0, 0, 3, 3), delay: 10);

            test.Tick(10);
            Assert.AreEqual(1, zone.mobs.Count);

            test.Tick();
            Assert.AreEqual(2, zone.mobs.Count);
        }

        [Test]
        public void Tick_AtMaxCount_StopsSpawning() {
            var zone = Zone(new RectInt(0, 0, 3, 3), maxCount: 3, delay: 0);

            test.Tick(20);

            Assert.AreEqual(3, zone.mobs.Count);
        }

        [Test]
        public void Tick_MobDespawned_ForgetsItAndRespawnsAfterTheDelay() {
            var zone = Zone(new RectInt(0, 0, 3, 3), maxCount: 1, delay: 10);
            test.Tick();
            var first = zone.mobs[0];

            test.Despawn(first);
            test.Tick();
            Assert.AreEqual(0, zone.mobs.Count);

            test.Tick(10);
            Assert.AreEqual(1, zone.mobs.Count);
            Assert.AreNotEqual(first, zone.mobs[0]);
        }

        [Test]
        public void Tick_OccupiedTile_SpawnsOnTheFreeOne() {
            test.Spawn(new Vector2Int(0, 0), Direction4.down);
            var zone = Zone(new RectInt(0, 0, 2, 1), maxCount: 2, delay: 0);

            test.Tick(5);

            Assert.AreEqual(1, zone.mobs.Count);
            Assert.AreEqual(new Vector2Int(1, 0), test.TileOf(zone.mobs[0]));
        }

        [Test]
        public void Tick_RegionOutsideEveryArea_SpawnsNothing() {
            // Outside every area counts as inactive, and its tiles are blocked anyway.
            var zone = Zone(new RectInt(100, 100, 2, 2), delay: 0);

            test.Tick(5);

            Assert.AreEqual(0, zone.mobs.Count);
        }

        [Test]
        public void Tick_SomeTilesOutOfView_SpawnsOutOfView() {
            var player = test.SpawnPlayer(Vector2Int.zero, Direction4.down);
            // A 3x3 view centred on the player shows x from -1 to 1.
            test.FollowWithCamera(player, new Vector2Int(3, 3));
            var zone = Zone(new RectInt(-1, 0, 4, 1), maxCount: 1);

            test.Tick();

            Assert.AreEqual(new Vector2Int(2, 0), test.TileOf(zone.mobs[0]));
        }

        [Test]
        public void Tick_EveryTileInView_SpawnsInViewAnyway() {
            var player = test.SpawnPlayer(Vector2Int.zero, Direction4.down);
            test.FollowWithCamera(player, new Vector2Int(3, 3));
            var zone = Zone(new RectInt(-1, 0, 2, 1), maxCount: 1);

            test.Tick();

            Assert.AreEqual(new Vector2Int(-1, 0), test.TileOf(zone.mobs[0]));
        }

        [Test]
        public void Tick_WanderAI_MobsStayInTheRegion() {
            var region = new RectInt(-2, -2, 4, 4);
            var zone = Zone(region, maxCount: 3, delay: 0, ai: new SpawnWanderAIData(true, 0, 5));

            for (int i = 0; i < 1000; i++) {
                test.Tick();
                foreach (var id in zone.mobs) {
                    Assert.IsTrue(region.Contains(test.TileOf(id)), $"{id} left the region at {test.TileOf(id)}.");
                }
            }
            Assert.AreEqual(3, zone.mobs.Count);
        }

        [Test]
        public void Tick_SpawnAIData_EachMobGetsItsOwnAI() {
            var zone = Zone(new RectInt(0, 0, 3, 3), maxCount: 2, delay: 0, ai: new SpawnSpinAIData(false, 10, 20));

            test.Tick(2);

            var a = test.world.components.ai.Get(zone.mobs[0]);
            var b = test.world.components.ai.Get(zone.mobs[1]);
            Assert.AreNotSame(a, b);
            Assert.IsFalse(a.pausesWhenInactive);
        }

        [Test]
        public void Tick_NoAI_MobHasNoAIComponent() {
            var zone = Zone(new RectInt(0, 0, 3, 3));

            test.Tick();

            Assert.IsFalse(test.world.components.ai.Has(zone.mobs[0]));
        }

        [Test]
        public void Tick_SubclassOverridesTheTile_SpawnsThere() {
            var zone = test.AddSpawnZone(new FixedTileZone(new RectInt(0, 0, 3, 3), test.definition, new Vector2Int(2, 1)));

            test.Tick();

            Assert.AreEqual(new Vector2Int(2, 1), test.TileOf(zone.mobs[0]));
        }

        [Test]
        public void Tick_SameSeed_SameSpawns() {
            var first = SpawnRun(7);
            var second = SpawnRun(7);

            CollectionAssert.AreEqual(first, second);
        }

        List<Vector2Int> SpawnRun(int seed) {
            using (var run = new TestWorld()) {
                run.SetAreaActive(true);
                run.world.GetContext<WorldSetupContext>().SeedRandom(seed);
                var zone = run.AddSpawnZone(new SpawnZone(new RectInt(-6, -6, 12, 12), run.definition, 5, 3, new SpawnWanderAIData(true, 0, 10)));
                run.Tick(300);

                var tiles = new List<Vector2Int>();
                foreach (var id in zone.mobs) {
                    tiles.Add(run.TileOf(id));
                }
                return tiles;
            }
        }

        [Test]
        public void Constructor_EmptyRegion_Throws() {
            Assert.Throws<ArgumentException>(() => new SpawnZone(new RectInt(0, 0, 0, 3), test.definition, 1, 0, null));
        }

        [Test]
        public void Constructor_ZeroMaxCount_Throws() {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SpawnZone(new RectInt(0, 0, 3, 3), test.definition, 0, 0, null));
        }

        [Test]
        public void IsTileInView_NoCameraController_False() {
            Assert.IsFalse(test.world.GetContext<SpawnContext>().IsTileInView(Vector2Int.zero));
        }

        [Test]
        public void IsTileInView_EdgesOfTheView_InsideOnly() {
            var player = test.SpawnPlayer(Vector2Int.zero, Direction4.down);
            test.FollowWithCamera(player, new Vector2Int(3, 3));
            var ctx = test.world.GetContext<SpawnContext>();

            Assert.IsTrue(ctx.IsTileInView(new Vector2Int(-1, 1)));
            Assert.IsTrue(ctx.IsTileInView(new Vector2Int(1, -1)));
            Assert.IsFalse(ctx.IsTileInView(new Vector2Int(2, 0)));
            Assert.IsFalse(ctx.IsTileInView(new Vector2Int(0, -2)));
            Assert.IsTrue(ctx.IsTileInView(new Vector2Int(2, 0), marginTiles: 1));
        }
    }
}
