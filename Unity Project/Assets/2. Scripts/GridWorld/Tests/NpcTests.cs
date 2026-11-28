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

        static void SetRegion(WanderAIBehaviour wander, RectInt region, TileSpace space = TileSpace.Area) {
            var properties = new SerializedObject(wander);
            properties.FindProperty("regionSpace").enumValueIndex = (int)space;
            properties.FindProperty("region").rectIntValue = region;
            properties.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Run(WorldInstance world, int ticks) {
            var phases = new PhaseExecutionManager();
            world.GetContext<AreaActivationContext>().SetAreaActive(0, true);
            for (int i = 0; i < ticks; i++) {
                phases.Tick(world);
            }
        }

        // Wanders for a while, checking the NPC never leaves the region and moves at least once.
        static void AssertStaysIn(WorldInstance world, Vector2Int start, RectInt region) {
            var id = world.occupancy.Get(start);
            var phases = new PhaseExecutionManager();
            world.GetContext<AreaActivationContext>().SetAreaActive(0, true);
            bool moved = false;
            for (int i = 0; i < 2000; i++) {
                phases.Tick(world);
                var tile = world.components.tile.Get(id);
                Assert.IsTrue(region.Contains(tile), $"Left {region} at {tile}.");
                moved |= tile != start;
            }
            Assert.IsTrue(moved);
        }

        // Points are offsets from the NPC.
        static void SetPoints(PatrolAIBehaviour patrol, params Vector2Int[] offsets) {
            var properties = new SerializedObject(patrol);
            var points = properties.FindProperty("points");
            points.arraySize = offsets.Length;
            for (int i = 0; i < offsets.Length; i++) {
                points.GetArrayElementAtIndex(i).boxedValue = new PatrolPointSettings(offsets[i], Direction4.Enum.none, 0);
            }
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

        // A 4x4 area at (0,0) where every tile can be entered except blocked.
        AreaDataSO AreaWithBlockedTile(Vector2Int blocked, params WorldFeatureData[] records) {
            var bounds = new RectInt(0, 0, 4, 4);
            var tiles = new ushort[bounds.width * bounds.height];
            for (int i = 0; i < tiles.Length; i++) {
                tiles[i] = TileBits.EnterAll;
            }
            tiles[blocked.x + blocked.y * bounds.width] = 0;

            var area = assets.Area("meadow", bounds, Vector2Int.zero);
            area.SetBakedData("meadow", "Assets/meadow.unity", bounds, Vector2Int.zero, tiles, new List<WorldFeatureData>(records));
            return area;
        }

        NpcData Patrol(Vector2Int tile, params Vector2Int[] pointTiles) {
            var points = new PatrolPointSettings[pointTiles.Length];
            for (int i = 0; i < pointTiles.Length; i++) {
                points[i] = new PatrolPointSettings(pointTiles[i], Direction4.Enum.none, 0);
            }
            return new NpcData(definition, tile, Direction4.Enum.down,
                new List<NpcAddonData> { new PatrolAIData(false, points, PatrolMode.Loop) });
        }

        void AssertLoadThrows(AreaDataSO area, string expected) {
            var error = Assert.Throws<InvalidOperationException>(() => {
                Load(area);
            });
            StringAssert.Contains("'meadow'", error.Message);
            StringAssert.Contains(expected, error.Message);
        }

        [Test]
        public void Load_NpcOutsideAreaBounds_ThrowsNamingArea() {
            var record = new NpcData(definition, new Vector2Int(4, 1), Direction4.Enum.down, null);
            var area = assets.Area("meadow", new RectInt(0, 0, 4, 4), Vector2Int.zero, TileBits.EnterAll, record);

            AssertLoadThrows(area, "outside the area's bounds");
        }

        [Test]
        public void Load_WanderRegionOutsideAreaBounds_ThrowsNamingArea() {
            var record = new NpcData(definition, new Vector2Int(1, 1), Direction4.Enum.down,
                new List<NpcAddonData> { new WanderAIData(false, new RectInt(0, 0, 5, 2), 0, 0) });
            var area = assets.Area("meadow", new RectInt(0, 0, 4, 4), Vector2Int.zero, TileBits.EnterAll, record);

            AssertLoadThrows(area, "wander region");
        }

        [Test]
        public void Load_WanderRegionFillingTheArea_Loads() {
            var record = new NpcData(definition, new Vector2Int(1, 1), Direction4.Enum.down,
                new List<NpcAddonData> { new WanderAIData(false, new RectInt(0, 0, 4, 4), 0, 0) });
            var area = assets.Area("meadow", new RectInt(0, 0, 4, 4), Vector2Int.zero, TileBits.EnterAll, record);

            var world = Load(area);

            Assert.AreEqual(1, world.components.ai.count);
        }

        [Test]
        public void Load_PatrolPointOutsideAreaBounds_ThrowsNamingArea() {
            var area = assets.Area("meadow", new RectInt(0, 0, 4, 4), Vector2Int.zero, TileBits.EnterAll,
                Patrol(new Vector2Int(1, 1), new Vector2Int(1, 1), new Vector2Int(5, 1)));

            AssertLoadThrows(area, "patrol point 1");
        }

        [Test]
        public void Load_TwoNpcsOnOneTile_ThrowsNamingArea() {
            var first = new NpcData(definition, new Vector2Int(2, 2), Direction4.Enum.down, null);
            var second = new NpcData(definition, new Vector2Int(2, 2), Direction4.Enum.up, null);
            var area = assets.Area("meadow", new RectInt(0, 0, 4, 4), Vector2Int.zero, TileBits.EnterAll, first, second);

            AssertLoadThrows(area, "already holds");
        }

        [Test]
        public void Load_NpcOnUnenterableTile_LogsWarningAndSpawns() {
            var record = new NpcData(definition, new Vector2Int(2, 1), Direction4.Enum.down, null);

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("'meadow'.*can't be entered from any direction"));
            var world = Load(AreaWithBlockedTile(new Vector2Int(2, 1), record));

            Assert.IsTrue(world.occupancy.Get(new Vector2Int(2, 1)).IsValid());
        }

        [Test]
        public void Load_PatrolPointOnUnenterableTile_LogsWarningAndSpawns() {
            var record = Patrol(new Vector2Int(0, 1), new Vector2Int(0, 1), new Vector2Int(3, 1));

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("'meadow'.*patrol point 1.*can't be entered"));
            var world = Load(AreaWithBlockedTile(new Vector2Int(3, 1), record));

            Assert.AreEqual(1, world.components.ai.count);
        }

        [Test]
        public void Load_InteractAddon_GivesTheNpcItsInteraction() {
            var cutscene = new SerializableTestCutscene("talk");
            var record = new NpcData(definition, new Vector2Int(1, 1), Direction4.Enum.down,
                new List<NpcAddonData> { new NpcInteractData(cutscene) });
            var area = assets.Area("meadow", new RectInt(0, 0, 4, 4), Vector2Int.zero, TileBits.EnterAll, record);

            var world = Load(area);

            var id = world.occupancy.Get(new Vector2Int(1, 1));
            Assert.AreSame(cutscene, world.interactions.entities.Get(id));
        }

        [Test]
        public void Load_InteractAddonWithNoCutscene_ThrowsNamingArea() {
            var record = new NpcData(definition, Vector2Int.zero, Direction4.Enum.down,
                new List<NpcAddonData> { new NpcInteractData(null) });
            var area = assets.Area("meadow", new RectInt(0, 0, 4, 4), Vector2Int.zero, TileBits.EnterAll, record);

            AssertLoadThrows(area, "no cutscene");
        }

        [Test]
        public void Load_NpcWithId_IsFoundByItsId() {
            var npc = Npc(new Vector2Int(2, 1));
            var properties = new SerializedObject(npc.GetComponent<NpcBehaviour>());
            properties.FindProperty("id").stringValue = "TEST_Greeter";
            properties.ApplyModifiedPropertiesWithoutUndo();
            var area = assets.Area("meadow", new RectInt(0, 0, 4, 4), Vector2Int.zero, TileBits.EnterAll, Bake().ToArray());

            var world = Load(area);

            Assert.IsTrue(world.featureId2entity.TryGetValue("TEST_Greeter", out var id));
            Assert.AreEqual(new Vector2Int(2, 1), world.components.tile.Get(id));
        }

        [Test]
        public void Bake_InteractWithNoCutscene_LogsErrorAndAddsNothing() {
            Npc(Vector2Int.zero).AddComponent<NpcInteractBehaviour>();

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("no cutscene"));
            CollectionAssert.IsEmpty(Bake());
        }

        [Test]
        public void Bake_InteractWithCutscene_RecordHoldsACopy() {
            var interact = Npc(Vector2Int.zero).AddComponent<NpcInteractBehaviour>();
            var original = new SerializableTestCutscene("talk");
            var properties = new SerializedObject(interact);
            properties.FindProperty("cutscene").managedReferenceValue = original;
            properties.ApplyModifiedPropertiesWithoutUndo();

            var records = Bake();

            var baked = (SerializableTestCutscene)((NpcInteractData)((NpcData)records[0]).addons[0]).cutscene;
            Assert.AreNotSame(original, baked);
            Assert.AreEqual("talk", baked.value);
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
            SetPoints(Npc(new Vector2Int(1, 1)).AddComponent<PatrolAIBehaviour>(), new Vector2Int(1, 1), new Vector2Int(4, 1));

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("first patrol point"));
            CollectionAssert.IsEmpty(Bake());
        }

        [Test]
        public void Bake_RelativeWanderRegion_BakesInAreaTiles() {
            SetRegion(Npc(new Vector2Int(5, 6)).AddComponent<WanderAIBehaviour>(), new RectInt(-1, -2, 3, 4), TileSpace.Relative);
            var area = assets.Area("meadow", new RectInt(0, 0, 10, 10), new Vector2Int(20, 0), TileBits.EnterAll, Bake().ToArray());

            var world = Load(area);

            // Area region (4,4) 3x4, plus the area's offset of (20,0).
            AssertStaysIn(world, new Vector2Int(25, 6), new RectInt(24, 4, 3, 4));
        }

        [Test]
        public void Bake_RelativeWanderRegionNotHoldingTheNpc_LogsError() {
            SetRegion(Npc(new Vector2Int(5, 6)).AddComponent<WanderAIBehaviour>(), new RectInt(1, 0, 2, 2), TileSpace.Relative);

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("relative to the NPC"));
            CollectionAssert.IsEmpty(Bake());
        }

        [Test]
        public void Bake_AreaWanderRegion_BakesUnchanged() {
            SetRegion(Npc(new Vector2Int(5, 6)).AddComponent<WanderAIBehaviour>(), new RectInt(4, 5, 3, 3));
            var area = assets.Area("meadow", new RectInt(0, 0, 10, 10), Vector2Int.zero, TileBits.EnterAll, Bake().ToArray());

            var world = Load(area);

            AssertStaysIn(world, new Vector2Int(5, 6), new RectInt(4, 5, 3, 3));
        }

        [Test]
        public void Bake_RelativePatrolPoints_WalksTheRouteFromTheNpc() {
            var npc = Npc(new Vector2Int(2, 3));
            SetPoints(npc.AddComponent<PatrolAIBehaviour>(), Vector2Int.zero, new Vector2Int(2, 0));
            var area = assets.Area("meadow", new RectInt(0, 0, 10, 10), new Vector2Int(20, 0), TileBits.EnterAll, Bake().ToArray());
            var world = Load(area);
            var id = world.occupancy.Get(new Vector2Int(22, 3));
            Assert.IsTrue(id.IsValid());

            // Point 1 is two tiles right of the NPC's world tile, (22,3).
            bool reached = false;
            for (int i = 0; i < 100 && !reached; i++) {
                Run(world, 1);
                reached = world.components.tile.Get(id) == new Vector2Int(24, 3);
            }
            Assert.IsTrue(reached);
        }

        [Test]
        public void Convert_ToRelativeAndBack_ReturnsTheSameRegion() {
            var region = new RectInt(4, -3, 5, 2);
            var origin = new Vector2Int(7, 1);

            var relative = TileSpaces.Convert(region, TileSpace.Area, TileSpace.Relative, origin);

            Assert.AreEqual(new RectInt(-3, -4, 5, 2), relative);
            Assert.AreEqual(region, TileSpaces.Convert(relative, TileSpace.Relative, TileSpace.Area, origin));
        }
    }
}
