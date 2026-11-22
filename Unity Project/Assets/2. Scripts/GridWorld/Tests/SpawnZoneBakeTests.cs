using System;
using System.Collections.Generic;
using LPE;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace GridWorld.Tests {
    public class SpawnZoneBakeTests {
        TestAssets assets;
        EntityDefinitionSO definition;
        GameObject gridObject;

        [SetUp]
        public void SetUp() {
            assets = new TestAssets();
            definition = assets.Create<EntityDefinitionSO>();
            gridObject = new GameObject("Grid", typeof(Grid));
        }

        [TearDown]
        public void TearDown() {
            UnityEngine.Object.DestroyImmediate(gridObject);
            assets.Dispose();
        }

        GameObject ZoneObject(RectInt region, EntityDefinitionSO zoneDefinition, TileSpace space = TileSpace.Area, Vector2Int tile = default) {
            var zoneObject = new GameObject("Zone");
            zoneObject.transform.SetParent(gridObject.transform, false);
            zoneObject.transform.position = new Vector3(tile.x + 0.5f, tile.y + 0.5f);

            var properties = new SerializedObject(zoneObject.AddComponent<SpawnZoneBehaviour>());
            properties.FindProperty("regionSpace").enumValueIndex = (int)space;
            properties.FindProperty("region").rectIntValue = region;
            properties.FindProperty("definition").objectReferenceValue = zoneDefinition;
            properties.ApplyModifiedPropertiesWithoutUndo();
            return zoneObject;
        }

        List<WorldFeatureData> Bake() {
            var records = new List<WorldFeatureData>();
            var baker = new WorldFeatureBaker("meadow", gridObject.GetComponent<Grid>(), records);
            foreach (var feature in gridObject.GetComponentsInChildren<WorldFeatureBehaviour>()) {
                feature.Bake(baker);
            }
            return records;
        }

        WorldInstance Load(AreaDataSO area) {
            var world = new WorldInstance();
            var catalogue = new WorldFeatureCatalogue();
            catalogue.Register(new SpawnZoneFeatureType<SpawnZoneData>());
            WorldStartup.Load(world.GetContext<WorldSetupContext>(), new[] { area }, catalogue);
            return world;
        }

        [Test]
        public void Bake_ZoneWithWanderAI_LoadsAZoneInWorldTiles() {
            ZoneObject(new RectInt(1, 2, 3, 4), definition).AddComponent<SpawnWanderAIBehaviour>();
            var records = Bake();
            var area = assets.Area("meadow", new RectInt(0, 0, 10, 10), new Vector2Int(20, 0), TileBits.EnterAll, records.ToArray());

            var world = Load(area);

            Assert.AreEqual(1, world.spawnZones.Count);
            var zone = world.spawnZones[0];
            Assert.AreEqual(new RectInt(21, 2, 3, 4), zone.region);
            Assert.IsInstanceOf<SpawnWanderAIData>(zone.ai);
        }

        [Test]
        public void Bake_RelativeRegion_LoadsInWorldTilesFromTheZoneObject() {
            ZoneObject(new RectInt(-1, -1, 3, 3), definition, TileSpace.Relative, new Vector2Int(3, 4));
            var records = Bake();
            var area = assets.Area("meadow", new RectInt(0, 0, 10, 10), new Vector2Int(20, 0), TileBits.EnterAll, records.ToArray());

            var world = Load(area);

            // Area region (2,3) 3x3, plus the area's offset of (20,0).
            Assert.AreEqual(new RectInt(22, 3, 3, 3), world.spawnZones[0].region);
        }

        [Test]
        public void Bake_NoDefinition_LogsAnError() {
            ZoneObject(new RectInt(1, 1, 3, 3), null);

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("no entity definition"));
            var records = Bake();

            Assert.AreEqual(0, records.Count);
        }

        [Test]
        public void Bake_TwoAIProfiles_LogsAnError() {
            var zoneObject = ZoneObject(new RectInt(1, 1, 3, 3), definition);
            zoneObject.AddComponent<SpawnWanderAIBehaviour>();
            zoneObject.AddComponent<SpawnSpinAIBehaviour>();

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("2 mob AI profiles"));
            var records = Bake();

            Assert.AreEqual(0, records.Count);
        }

        [Test]
        public void Load_RegionOutsideTheArea_Throws() {
            var record = new SpawnZoneData(new RectInt(8, 8, 4, 4), definition, 3, 10, null);
            var area = assets.Area("meadow", new RectInt(0, 0, 10, 10), Vector2Int.zero, TileBits.EnterAll, record);

            Assert.Throws<InvalidOperationException>(() => Load(area));
        }
    }
}
