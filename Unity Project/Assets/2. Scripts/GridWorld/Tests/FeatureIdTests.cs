using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GridWorld.Tests {
    public class FeatureIdTests {
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

        NpcBehaviour Npc(string id, Vector2Int tile) {
            var npcObject = new GameObject($"Npc {id}");
            npcObject.transform.SetParent(gridObject.transform, false);
            npcObject.transform.position = new Vector3(tile.x + 0.5f, tile.y + 0.5f);

            var npc = npcObject.AddComponent<NpcBehaviour>();
            var properties = new SerializedObject(npc);
            properties.FindProperty("id").stringValue = id;
            properties.FindProperty("definition").objectReferenceValue = definition;
            properties.ApplyModifiedPropertiesWithoutUndo();
            return npc;
        }

        // Bakes the NPCs made so far into an area, then clears them for the next area.
        AreaDataSO BakeArea(string areaId, Vector2Int offset) {
            var records = new List<WorldFeatureData>();
            var baker = new WorldFeatureBaker(areaId, gridObject.GetComponent<Grid>(), records);
            foreach (var feature in gridObject.GetComponentsInChildren<WorldFeatureBehaviour>()) {
                feature.Bake(baker);
            }
            for (int i = gridObject.transform.childCount - 1; i >= 0; i--) {
                UnityEngine.Object.DestroyImmediate(gridObject.transform.GetChild(i).gameObject);
            }
            return assets.Area(areaId, new RectInt(0, 0, 10, 10), offset, TileBits.EnterAll, records.ToArray());
        }

        [TestCase("guard_1")]
        [TestCase("Mob-Zone-2")]
        public void FindFormatError_AllowedCharacters_ReturnsNull(string id) {
            Assert.IsNull(FeatureIds.FindFormatError(id));
        }

        [TestCase("")]
        [TestCase("guard 1")]
        [TestCase("guard.1")]
        [TestCase("gärd")]
        public void FindFormatError_EmptyOrOtherCharacters_ReturnsAnError(string id) {
            Assert.IsNotNull(FeatureIds.FindFormatError(id));
        }

        [Test]
        public void Same_DifferentCase_ReturnsTrue() {
            Assert.IsTrue(FeatureIds.Same("Guard_1", "guard_1"));
            Assert.IsFalse(FeatureIds.Same("guard_1", "guard_2"));
        }

        [Test]
        public void Bake_NpcWithAnId_CopiesItOntoTheRecord() {
            Npc("guard_1", new Vector2Int(2, 3));

            var area = BakeArea("meadow", Vector2Int.zero);

            Assert.AreEqual("guard_1", ((NpcData)area.featureRecords[0]).id);
        }

        [Test]
        public void Checks_DuplicateIgnoringCase_IsAnError() {
            var features = new List<IdentifiedFeatureBehaviour> { Npc("guard_1", Vector2Int.zero), Npc("GUARD_1", Vector2Int.one) };

            var problems = FeatureIdChecks.Find(features);

            Assert.AreEqual(1, problems.Count);
            Assert.IsTrue(problems[0].isError);
            StringAssert.Contains("share the ID", problems[0].message);
        }

        [Test]
        public void Checks_MissingId_IsAWarning() {
            var problems = FeatureIdChecks.Find(new List<IdentifiedFeatureBehaviour> { Npc("", Vector2Int.zero) });

            Assert.AreEqual(1, problems.Count);
            Assert.IsFalse(problems[0].isError);
        }

        [Test]
        public void Checks_BadFormat_IsAnError() {
            var problems = FeatureIdChecks.Find(new List<IdentifiedFeatureBehaviour> { Npc("guard 1", Vector2Int.zero) });

            Assert.AreEqual(1, problems.Count);
            Assert.IsTrue(problems[0].isError);
        }

        [Test]
        public void Checks_UniqueIds_FindNothing() {
            var features = new List<IdentifiedFeatureBehaviour> { Npc("guard_1", Vector2Int.zero), Npc("guard_2", Vector2Int.one) };

            CollectionAssert.IsEmpty(FeatureIdChecks.Find(features));
        }

        [Test]
        public void FindError_SameIdInTwoAreas_NamesBothAreas() {
            Npc("guard_1", Vector2Int.zero);
            var a = BakeArea("meadow", Vector2Int.zero);
            Npc("Guard_1", Vector2Int.zero);
            var b = BakeArea("forest", new Vector2Int(10, 0));

            var error = WorldValidation.FindError(new[] { a, b });

            StringAssert.Contains("'meadow'", error);
            StringAssert.Contains("'forest'", error);
        }

        [Test]
        public void FindError_SameIdTwiceInOneArea_ReturnsAnError() {
            // The bake refuses this, so it only comes from stale or hand-made data.
            Npc("guard_1", Vector2Int.zero);
            Npc("guard_1", Vector2Int.one);
            var area = BakeArea("meadow", Vector2Int.zero);

            StringAssert.Contains("two features", WorldValidation.FindError(new[] { area }));
        }

        [Test]
        public void FindError_FeaturesWithoutIds_ReturnsNull() {
            Npc("", Vector2Int.zero);
            Npc("", Vector2Int.one);
            var area = BakeArea("meadow", Vector2Int.zero);

            Assert.IsNull(WorldValidation.FindError(new[] { area }));
        }

        [Test]
        public void Load_SameIdInTwoAreas_Throws() {
            Npc("guard_1", Vector2Int.zero);
            var a = BakeArea("meadow", Vector2Int.zero);
            Npc("guard_1", Vector2Int.zero);
            var b = BakeArea("forest", new Vector2Int(10, 0));

            var world = new WorldInstance();
            var catalogue = new WorldFeatureCatalogue();
            catalogue.Register(new NpcFeatureType());

            Assert.Throws<InvalidOperationException>(() => {
                WorldStartup.Load(world.GetContext<WorldSetupContext>(), new[] { a, b }, catalogue);
            });
        }
    }
}
