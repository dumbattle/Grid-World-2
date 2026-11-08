using System;
using System.Collections.Generic;
using LPE;
using NUnit.Framework;
using UnityEngine;

namespace GridWorld.Tests {
    public class WorldStartupTests {
        class PointData : WorldFeatureData {
            public Vector2Int tile;
        }

        class UnknownData : WorldFeatureData { }

        class PointType : WorldFeatureType<PointData> {
            public readonly List<Vector2Int> worldTiles = new List<Vector2Int>();

            protected override void Register(WorldSetupContext ctx, AreaDataSO area, PointData data) {
                worldTiles.Add(area.ToWorldTile(data.tile));
            }
        }

        TestAssets assets;
        WorldInstance world;
        WorldSetupContext ctx;
        WorldFeatureCatalogue catalogue;
        PointType points;

        [SetUp]
        public void SetUp() {
            assets = new TestAssets();
            world = new WorldInstance();
            ctx = world.GetContext<WorldSetupContext>();
            catalogue = new WorldFeatureCatalogue();
            points = new PointType();
            catalogue.Register(points);
            catalogue.Register(new PlayerSpawnFeatureType());
        }

        [TearDown]
        public void TearDown() {
            assets.Dispose();
        }

        [Test]
        public void Load_OffsetArea_PlacesTilesAtWorldPosition() {
            var area = assets.Area("interior", new RectInt(0, 0, 4, 4), new Vector2Int(100, 50));

            WorldStartup.Load(ctx, new[] { area }, catalogue);

            Assert.IsTrue(world.tiles.CanEnter(new Vector2Int(100, 50), Direction4.up));
            Assert.IsTrue(world.tiles.CanEnter(new Vector2Int(103, 53), Direction4.up));
            Assert.IsFalse(world.tiles.CanEnter(new Vector2Int(0, 0), Direction4.up));
            Assert.IsFalse(world.tiles.CanEnter(new Vector2Int(104, 50), Direction4.up));
        }

        [Test]
        public void Load_FeatureRecord_ReceivesWorldTile() {
            var area = assets.Area("interior", new RectInt(0, 0, 4, 4), new Vector2Int(100, 50), TileBits.EnterAll,
                new PointData { tile = new Vector2Int(1, 2) });

            WorldStartup.Load(ctx, new[] { area }, catalogue);

            CollectionAssert.AreEqual(new[] { new Vector2Int(101, 52) }, points.worldTiles);
        }

        [Test]
        public void Load_UnknownRecordType_ThrowsNamingAreaAndType() {
            var area = assets.Area("meadow", new RectInt(0, 0, 4, 4), Vector2Int.zero, TileBits.EnterAll, new UnknownData());

            var error = Assert.Throws<InvalidOperationException>(() => {
                WorldStartup.Load(ctx, new[] { area }, catalogue);
            });

            StringAssert.Contains("'meadow'", error.Message);
            StringAssert.Contains(nameof(UnknownData), error.Message);
        }

        [Test]
        public void Load_MissingRecordClass_ThrowsNamingArea() {
            var area = assets.Area("meadow", new RectInt(0, 0, 4, 4), Vector2Int.zero, TileBits.EnterAll, new WorldFeatureData[] { null });

            var error = Assert.Throws<InvalidOperationException>(() => {
                WorldStartup.Load(ctx, new[] { area }, catalogue);
            });

            StringAssert.Contains("'meadow'", error.Message);
        }

        [Test]
        public void Load_OverlappingAreas_ThrowsBeforeAddingAnything() {
            var a = assets.Area("a", new RectInt(0, 0, 4, 4), Vector2Int.zero);
            var b = assets.Area("b", new RectInt(2, 2, 4, 4), Vector2Int.zero);

            Assert.Throws<InvalidOperationException>(() => {
                WorldStartup.Load(ctx, new[] { a, b }, catalogue);
            });
            Assert.IsFalse(world.tiles.CanEnter(Vector2Int.zero, Direction4.up));
        }

        [Test]
        public void Load_PlayerSpawn_CreatesPlayerAtWorldTile() {
            var definition = assets.Create<EntityDefinitionSO>();
            var spawn = new PlayerSpawnData(definition, new Vector2Int(2, 1), Direction4.Enum.left);
            var area = assets.Area("interior", new RectInt(0, 0, 4, 4), new Vector2Int(10, 0), TileBits.EnterAll, spawn);

            WorldStartup.Load(ctx, new[] { area }, catalogue);

            var player = world.player;
            Assert.IsTrue(player.IsValid());
            Assert.AreEqual(new Vector2Int(12, 1), world.components.tile.Get(player));
            Assert.AreSame(Direction4.left, world.components.facing.Get(player));
            Assert.AreSame(definition, world.components.definition.Get(player));
            Assert.AreEqual(player, world.occupancy.Get(new Vector2Int(12, 1)));
        }

        [Test]
        public void Load_SecondPlayerSpawn_ThrowsNamingArea() {
            var definition = assets.Create<EntityDefinitionSO>();
            var a = assets.Area("a", new RectInt(0, 0, 4, 4), Vector2Int.zero, TileBits.EnterAll,
                new PlayerSpawnData(definition, Vector2Int.zero, Direction4.Enum.down));
            var b = assets.Area("b", new RectInt(4, 0, 4, 4), Vector2Int.zero, TileBits.EnterAll,
                new PlayerSpawnData(definition, new Vector2Int(5, 0), Direction4.Enum.down));

            var error = Assert.Throws<InvalidOperationException>(() => {
                WorldStartup.Load(ctx, new[] { a, b }, catalogue);
            });

            StringAssert.Contains("'b'", error.Message);
        }
    }
}
