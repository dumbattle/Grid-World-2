using System;
using System.Collections.Generic;
using LPE;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace GridWorld.Tests {
    public class NpcTests {
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

        WorldInstance Load(AreaDataSO area) {
            var world = new WorldInstance();
            var catalogue = new WorldFeatureCatalogue();
            catalogue.Register(new NpcFeatureType());
            WorldStartup.Load(world.GetContext<WorldSetupContext>(), new[] { area }, catalogue);
            return world;
        }

        GameObject Npc(Vector2Int tile) {
            var npcObject = new GameObject("Npc");
            npcObject.transform.SetParent(gridObject.transform, false);
            npcObject.transform.position = new Vector3(tile.x + 0.5f, tile.y + 0.5f);

            var properties = new SerializedObject(npcObject.AddComponent<NpcBehaviour>());
            properties.FindProperty("definition").objectReferenceValue = definition;
            properties.ApplyModifiedPropertiesWithoutUndo();
            return npcObject;
        }

        List<WorldFeatureData> Bake() {
            var records = new List<WorldFeatureData>();
            var baker = new WorldFeatureBaker("meadow", gridObject.GetComponent<Grid>(), records);
            foreach (var feature in gridObject.GetComponentsInChildren<WorldFeatureBehaviour>()) {
                feature.Bake(baker);
            }
            return records;
        }

        static void SetRegion(WanderAIBehaviour wander, RectInt region) {
            var properties = new SerializedObject(wander);
            properties.FindProperty("region").rectIntValue = region;
            properties.ApplyModifiedPropertiesWithoutUndo();
        }

        [Test]
        public void Load_NpcRecord_SpawnsAtWorldTileWithItsAI() {
            var record = new NpcData(definition, new Vector2Int(1, 2), Direction4.Enum.left,
                new List<NpcAddonData> { new SpinAIData(false, 0, 0) });
            var area = assets.Area("interior", new RectInt(0, 0, 4, 4), new Vector2Int(100, 50), TileBits.EnterAll, record);

            var world = Load(area);

            var id = world.occupancy.Get(new Vector2Int(101, 52));
            Assert.IsTrue(id.IsValid());
            Assert.AreEqual(Direction4.left, world.components.facing.Get(id));
            Assert.IsInstanceOf<SpinAI>(world.components.ai.Get(id));
            Assert.IsFalse(world.components.ai.Get(id).pausesWhenInactive);
        }

        [Test]
        public void Load_NpcWithNoAddons_HasNoAI() {
            var record = new NpcData(definition, Vector2Int.zero, Direction4.Enum.down, null);
            var area = assets.Area("meadow", new RectInt(0, 0, 4, 4), Vector2Int.zero, TileBits.EnterAll, record);

            var world = Load(area);

            Assert.AreEqual(1, world.components.tile.count);
            Assert.AreEqual(0, world.components.ai.count);
        }

        [Test]
        public void Load_MissingAddonClass_ThrowsNamingArea() {
            var record = new NpcData(definition, Vector2Int.zero, Direction4.Enum.down, new List<NpcAddonData> { null });
            var area = assets.Area("meadow", new RectInt(0, 0, 4, 4), Vector2Int.zero, TileBits.EnterAll, record);

            var error = Assert.Throws<InvalidOperationException>(() => {
                Load(area);
            });

            StringAssert.Contains("'meadow'", error.Message);
        }

        [Test]
        public void Bake_NpcWithWander_AddsOneRecordWithItsAI() {
            SetRegion(Npc(new Vector2Int(3, 4)).AddComponent<WanderAIBehaviour>(), new RectInt(2, 3, 3, 3));

            var records = Bake();

            Assert.AreEqual(1, records.Count);
            var npc = (NpcData)records[0];
            Assert.AreEqual(new Vector2Int(3, 4), npc.tile);
            Assert.AreEqual(1, npc.addons.Count);
            Assert.IsInstanceOf<WanderAIData>(npc.addons[0]);
        }

        [Test]
        public void Bake_TwoAIProfiles_LogsErrorAndAddsNothing() {
            var npc = Npc(Vector2Int.zero);
            npc.AddComponent<SpinAIBehaviour>();
            SetRegion(npc.AddComponent<WanderAIBehaviour>(), new RectInt(0, 0, 2, 2));

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("2 AI profiles"));
            CollectionAssert.IsEmpty(Bake());
        }

        [Test]
        public void Bake_NpcOutsideWanderRegion_LogsErrorAndAddsNothing() {
            SetRegion(Npc(new Vector2Int(9, 9)).AddComponent<WanderAIBehaviour>(), new RectInt(0, 0, 2, 2));

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("outside its wander region"));
            CollectionAssert.IsEmpty(Bake());
        }

        [Test]
        public void Bake_NpcOutOfLineWithFirstPatrolPoint_LogsErrorAndAddsNothing() {
            var patrol = Npc(new Vector2Int(1, 1)).AddComponent<PatrolAIBehaviour>();
            var properties = new SerializedObject(patrol);
            var points = properties.FindProperty("points");
            points.arraySize = 2;
            points.GetArrayElementAtIndex(0).boxedValue = new PatrolPointSettings(new Vector2Int(0, 0), Direction4.Enum.none, 0);
            points.GetArrayElementAtIndex(1).boxedValue = new PatrolPointSettings(new Vector2Int(3, 0), Direction4.Enum.none, 0);
            properties.ApplyModifiedPropertiesWithoutUndo();

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("first patrol point"));
            CollectionAssert.IsEmpty(Bake());
        }
    }
}
