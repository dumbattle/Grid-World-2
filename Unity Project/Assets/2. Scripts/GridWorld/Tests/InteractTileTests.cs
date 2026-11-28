using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace GridWorld.Tests {
    public class InteractTileTests {
        TestAssets assets;
        GameObject gridObject;

        [SetUp]
        public void SetUp() {
            assets = new TestAssets();
            gridObject = new GameObject("Grid", typeof(Grid));
        }

        [TearDown]
        public void TearDown() {
            UnityEngine.Object.DestroyImmediate(gridObject);
            assets.Dispose();
        }

        static WorldInstance Load(AreaDataSO area) {
            var world = new WorldInstance();
            var catalogue = new WorldFeatureCatalogue();
            catalogue.Register(new InteractTileFeatureType());
            WorldStartup.Load(world.GetContext<WorldSetupContext>(), new[] { area }, catalogue);
            return world;
        }

        AreaDataSO Area(params WorldFeatureData[] records) {
            return assets.Area("meadow", new RectInt(0, 0, 4, 4), new Vector2Int(100, 50), TileBits.EnterAll, records);
        }

        static void AssertLoadThrows(AreaDataSO area, string expected) {
            var error = Assert.Throws<InvalidOperationException>(() => {
                Load(area);
            });
            StringAssert.Contains("'meadow'", error.Message);
            StringAssert.Contains(expected, error.Message);
        }

        InteractTileBehaviour Feature(Vector2Int tile, Vector2Int size, ICutscene cutscene) {
            var featureObject = new GameObject("Interact Tile");
            featureObject.transform.SetParent(gridObject.transform, false);
            featureObject.transform.position = new Vector3(tile.x + 0.5f, tile.y + 0.5f);

            var feature = featureObject.AddComponent<InteractTileBehaviour>();
            var properties = new SerializedObject(feature);
            properties.FindProperty("size").vector2IntValue = size;
            properties.FindProperty("cutscene").managedReferenceValue = cutscene;
            properties.ApplyModifiedPropertiesWithoutUndo();
            return feature;
        }

        List<WorldFeatureData> Bake() {
            var records = new List<WorldFeatureData>();
            var baker = new WorldFeatureBaker("meadow", gridObject.GetComponent<Grid>(), records);
            foreach (var feature in gridObject.GetComponentsInChildren<WorldFeatureBehaviour>()) {
                feature.Bake(baker);
            }
            return records;
        }

        [Test]
        public void Load_TwoByOneTile_AddsTheCutsceneToBothWorldTiles() {
            var cutscene = new SerializableTestCutscene("sign");
            var world = Load(Area(new InteractTileData(new Vector2Int(1, 1), new Vector2Int(2, 1), cutscene)));

            Assert.AreSame(cutscene, world.tile2interaction[new Vector2Int(101, 51)]);
            Assert.AreSame(cutscene, world.tile2interaction[new Vector2Int(102, 51)]);
            Assert.AreEqual(2, world.tile2interaction.Count);
        }

        [Test]
        public void Load_TwoTilesOverlap_ThrowsNamingArea() {
            var first = new InteractTileData(new Vector2Int(0, 0), new Vector2Int(2, 2), new SerializableTestCutscene());
            var second = new InteractTileData(new Vector2Int(1, 1), Vector2Int.one, new SerializableTestCutscene());

            AssertLoadThrows(Area(first, second), "already has an interaction");
        }

        [Test]
        public void Load_TileReachesOutsideBounds_ThrowsNamingArea() {
            var record = new InteractTileData(new Vector2Int(3, 0), new Vector2Int(2, 1), new SerializableTestCutscene());

            AssertLoadThrows(Area(record), "outside the area's bounds");
        }

        [Test]
        public void Load_TileWithNoCutscene_ThrowsNamingArea() {
            var record = new InteractTileData(Vector2Int.zero, Vector2Int.one, null);

            AssertLoadThrows(Area(record), "no cutscene");
        }

        [Test]
        public void Bake_TileWithNoCutscene_LogsErrorAndAddsNothing() {
            Feature(Vector2Int.zero, Vector2Int.one, null);

            LogAssert.Expect(LogType.Error, new Regex("no cutscene"));
            CollectionAssert.IsEmpty(Bake());
        }

        [Test]
        public void Bake_TileWithZeroWidth_LogsErrorAndAddsNothing() {
            Feature(Vector2Int.zero, new Vector2Int(0, 1), new SerializableTestCutscene());

            LogAssert.Expect(LogType.Error, new Regex("size"));
            CollectionAssert.IsEmpty(Bake());
        }

        [Test]
        public void Bake_Tile_RecordsItsTilesAndACopyOfItsCutscene() {
            var original = new SerializableTestCutscene("sign");
            Feature(new Vector2Int(2, 3), new Vector2Int(2, 1), original);

            var records = Bake();

            var record = (InteractTileData)records[0];
            Assert.AreEqual(new RectInt(2, 3, 2, 1), record.tiles);
            Assert.AreNotSame(original, record.cutscene);
            Assert.AreEqual("sign", ((SerializableTestCutscene)record.cutscene).value);
        }
    }
}
