using System.Collections.Generic;
using LPE;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GridWorld.Tests {
    public class FeatureEditApplierTests {
        static readonly RectInt bounds = new RectInt(0, 0, 20, 20);

        TestAssets assets;
        EntityDefinitionSO definition;
        GameObject gridObject;

        [SetUp]
        public void SetUp() {
            assets = new TestAssets();
            definition = assets.Create<EntityDefinitionSO>();
            gridObject = new GameObject("TEST_Edit Grid", typeof(Grid));
        }

        [TearDown]
        public void TearDown() {
            if (gridObject != null) {
                Object.DestroyImmediate(gridObject);
            }
            assets.Dispose();
        }

        NpcBehaviour Npc(string id, Vector2Int tile) {
            var npcObject = new GameObject($"TEST_Npc {id}");
            npcObject.transform.SetParent(gridObject.transform, false);
            npcObject.transform.position = new Vector3(tile.x + 0.3f, tile.y + 0.7f, 2f);

            var npc = npcObject.AddComponent<NpcBehaviour>();
            var properties = new SerializedObject(npc);
            properties.FindProperty("id").stringValue = id;
            properties.FindProperty("definition").objectReferenceValue = definition;
            properties.FindProperty("facing").enumValueIndex = (int)Direction4.Enum.down;
            properties.ApplyModifiedPropertiesWithoutUndo();
            return npc;
        }

        static WanderAIBehaviour AddWander(NpcBehaviour npc, TileSpace space, RectInt region) {
            var wander = npc.gameObject.AddComponent<WanderAIBehaviour>();
            var properties = new SerializedObject(wander);
            properties.FindProperty("regionSpace").enumValueIndex = (int)space;
            properties.FindProperty("region").rectIntValue = region;
            properties.ApplyModifiedPropertiesWithoutUndo();
            return wander;
        }

        static PatrolAIBehaviour AddPatrol(NpcBehaviour npc, params Vector2Int[] tiles) {
            var patrol = npc.gameObject.AddComponent<PatrolAIBehaviour>();
            var properties = new SerializedObject(patrol);
            var points = properties.FindProperty("points");
            points.arraySize = tiles.Length;
            for (int i = 0; i < tiles.Length; i++) {
                points.GetArrayElementAtIndex(i).FindPropertyRelative("tile").vector2IntValue = tiles[i];
            }
            properties.ApplyModifiedPropertiesWithoutUndo();
            return patrol;
        }

        FeatureEditPlan Evaluate(string edits) {
            var errors = new List<string>();
            var file = FeatureEditParser.Parse("area: meadow\nworld: Assets/World.asset\n" + edits, "edits.txt", errors);
            CollectionAssert.IsEmpty(errors, "The test's edit file should parse.");

            var plan = new FeatureEditPlan { file = file };
            var features = new List<IdentifiedFeatureBehaviour>(gridObject.GetComponentsInChildren<IdentifiedFeatureBehaviour>(true));
            FeatureEditApplier.Evaluate(plan, features, gridObject.GetComponent<Grid>(), bounds);
            return plan;
        }

        static Vector2Int TileOf(Component component) {
            return TileSpaces.TileOf(component.transform);
        }

        static string Field(Object target, string path) {
            return FeatureEditValues.Describe(new SerializedObject(target).FindProperty(path));
        }

        [Test]
        public void Evaluate_MoveFromTheCurrentTile_IsAChange() {
            Npc("guard_1", new Vector2Int(3, 4));

            var plan = Evaluate("move guard_1 from 3 4 to 3 5\n");

            Assert.AreEqual(FeatureEditStatus.Change, plan.results[0].status);
            Assert.IsTrue(plan.hasChanges);
        }

        [Test]
        public void Evaluate_MoveWhenAlreadyThere_IsAlreadyApplied() {
            Npc("guard_1", new Vector2Int(3, 5));

            var plan = Evaluate("move guard_1 from 3 4 to 3 5\n");

            Assert.AreEqual(FeatureEditStatus.AlreadyApplied, plan.results[0].status);
            Assert.IsFalse(plan.hasChanges);
        }

        [Test]
        public void Evaluate_MoveFromAStaleTile_Conflicts() {
            Npc("guard_1", new Vector2Int(8, 8));

            var plan = Evaluate("move guard_1 from 3 4 to 3 5\n");

            Assert.AreEqual(FeatureEditStatus.Conflict, plan.results[0].status);
            StringAssert.Contains("is at 8 8", plan.results[0].message);
        }

        [Test]
        public void Evaluate_UnknownId_Conflicts() {
            Npc("guard_1", new Vector2Int(3, 4));

            var plan = Evaluate("move guard_2 from 3 4 to 3 5\n");

            Assert.AreEqual(FeatureEditStatus.Conflict, plan.results[0].status);
        }

        [Test]
        public void Evaluate_IdOnTwoFeatures_Conflicts() {
            Npc("guard_1", new Vector2Int(3, 4));
            Npc("GUARD_1", new Vector2Int(6, 4));

            var plan = Evaluate("move guard_1 from 3 4 to 3 5\n");

            Assert.AreEqual(FeatureEditStatus.Conflict, plan.results[0].status);
        }

        [Test]
        public void Evaluate_OneConflict_BlocksTheWholeFile() {
            Npc("guard_1", new Vector2Int(3, 4));

            var plan = Evaluate("move guard_1 from 3 4 to 3 5\nface guard_2 from down to up\n");

            Assert.AreEqual(FeatureEditStatus.Change, plan.results[0].status);
            Assert.IsTrue(plan.hasConflicts);
            Assert.IsFalse(plan.hasChanges);
            Assert.Throws<System.InvalidOperationException>(() => FeatureEditApplier.Commit(plan));
            Assert.AreEqual(new Vector2Int(3, 4), TileOf(gridObject.transform.GetChild(0)));
        }

        [Test]
        public void Evaluate_SetWithAStaleValue_Conflicts() {
            var npc = Npc("guard_1", new Vector2Int(3, 4));
            AddWander(npc, TileSpace.Relative, new RectInt(-2, -2, 5, 5));

            var plan = Evaluate("set guard_1 WanderAIBehaviour.minWaitTicks from 10 to 20\n");

            Assert.AreEqual(FeatureEditStatus.Conflict, plan.results[0].status);
            StringAssert.Contains("is 30, not 10", plan.results[0].message);
        }

        [TestCase("set guard_1 WanderAIBehaviour.speed from 1 to 2")]
        [TestCase("set guard_1 SpinAIBehaviour.minWaitTicks from 30 to 40")]
        [TestCase("set guard_1 WanderAIBehaviour.minWaitTicks from 30 to fast")]
        [TestCase("set guard_1 WanderAIBehaviour.region from -2 -2 5 5 to 1 1")]
        [TestCase("set guard_1 WanderAIBehaviour.regionSpace from Relative to Sideways")]
        [TestCase("set guard_1 WanderAIBehaviour.m_Enabled from true to false")]
        [TestCase("set guard_1 NpcBehaviour.id from guard_1 to guard_2")]
        [TestCase("set guard_1 NpcBehaviour.definition from none to \"Assets/TEST_Missing.asset\"")]
        [TestCase("face guard_1 from down to none")]
        public void Evaluate_EditThatCantApply_Conflicts(string edit) {
            var npc = Npc("guard_1", new Vector2Int(3, 4));
            AddWander(npc, TileSpace.Relative, new RectInt(-2, -2, 5, 5));

            var plan = Evaluate(edit + "\n");

            Assert.AreEqual(FeatureEditStatus.Conflict, plan.results[0].status, plan.results[0].message);
        }

        [Test]
        public void Evaluate_MoveNpcWithAnAreaRegion_Warns() {
            var npc = Npc("guard_1", new Vector2Int(3, 4));
            AddWander(npc, TileSpace.Area, new RectInt(1, 2, 5, 5));

            var plan = Evaluate("move guard_1 from 3 4 to 3 5\n");

            Assert.AreEqual(1, plan.results[0].warnings.Count);
            StringAssert.Contains("area tiles", plan.results[0].warnings[0]);
        }

        [Test]
        public void Evaluate_MovePatrollingNpcOutsideTheBounds_WarnsAboutBoth() {
            var npc = Npc("guard_1", new Vector2Int(19, 4));
            AddPatrol(npc, Vector2Int.zero, new Vector2Int(-3, 0));

            var plan = Evaluate("move guard_1 from 19 4 to 20 4\n");

            Assert.AreEqual(2, plan.results[0].warnings.Count);
        }

        [Test]
        public void Evaluate_RemoveObjectWithChildren_Conflicts() {
            var npc = Npc("guard_1", new Vector2Int(3, 4));
            new GameObject("TEST_Child").transform.SetParent(npc.transform);

            var plan = Evaluate("remove guard_1 at 3 4\n");

            Assert.AreEqual(FeatureEditStatus.Conflict, plan.results[0].status);
        }

        [Test]
        public void Evaluate_RemoveFromAnotherTile_Conflicts() {
            Npc("guard_1", new Vector2Int(3, 4));

            var plan = Evaluate("remove guard_1 at 5 5\n");

            Assert.AreEqual(FeatureEditStatus.Conflict, plan.results[0].status);
        }

        [Test]
        public void Evaluate_RemoveMissingId_IsAlreadyApplied() {
            var plan = Evaluate("remove guard_1 at 3 4\n");

            Assert.AreEqual(FeatureEditStatus.AlreadyApplied, plan.results[0].status);
        }

        [Test]
        public void Commit_EveryKindOfEdit_ChangesTheScene() {
            var guard = Npc("guard_1", new Vector2Int(3, 4));
            var patrol = AddPatrol(guard, Vector2Int.zero, new Vector2Int(0, 3));
            Npc("mob_old", new Vector2Int(9, 9));

            var plan = Evaluate(
                "move guard_1 from 3 4 to 4 6\n" +
                "face guard_1 from down to left\n" +
                "set guard_1 PatrolAIBehaviour.points[1].tile from 0 3 to 0 4\n" +
                "set guard_1 PatrolAIBehaviour.mode from Loop to PingPong\n" +
                "remove mob_old at 9 9\n");
            Assert.IsTrue(plan.hasChanges, string.Join("\n", plan.results.ConvertAll(r => r.message)));
            FeatureEditApplier.Commit(plan);

            Assert.AreEqual(new Vector3(4.5f, 6.5f, 2f), guard.transform.position);
            Assert.AreEqual("left", Field(guard, "facing"));
            Assert.AreEqual("0 4", Field(patrol, "points.Array.data[1].tile"));
            Assert.AreEqual("PingPong", Field(patrol, "mode"));
            Assert.AreEqual(1, gridObject.transform.childCount);
        }

        [Test]
        public void Commit_ThenUndo_RestoresEverything() {
            var guard = Npc("guard_1", new Vector2Int(3, 4));
            Npc("mob_old", new Vector2Int(9, 9));
            var start = guard.transform.position;

            var plan = Evaluate("move guard_1 from 3 4 to 4 6\nface guard_1 from down to left\nremove mob_old at 9 9\n");
            FeatureEditApplier.Commit(plan);
            Undo.PerformUndo();

            Assert.AreEqual(start, guard.transform.position);
            Assert.AreEqual("down", Field(guard, "facing"));
            Assert.AreEqual(2, gridObject.transform.childCount);
        }

        [Test]
        public void Commit_ThenEvaluateAgain_FindsEverythingAlreadyApplied() {
            var guard = Npc("guard_1", new Vector2Int(3, 4));
            AddWander(guard, TileSpace.Relative, new RectInt(-2, -2, 5, 5));
            Npc("mob_old", new Vector2Int(9, 9));
            const string Edits =
                "move guard_1 from 3 4 to 4 6\n" +
                "set guard_1 WanderAIBehaviour.region from -2 -2 5 5 to -1 -1 3 3\n" +
                "set guard_1 WanderAIBehaviour.pauseWhenAreaInactive from true to false\n" +
                "remove mob_old at 9 9\n";

            FeatureEditApplier.Commit(Evaluate(Edits));
            var again = Evaluate(Edits);

            Assert.AreEqual(4, again.Count(FeatureEditStatus.AlreadyApplied));
            Assert.IsFalse(again.hasChanges);
        }

        [Test]
        public void Commit_GrowListThenSetItsNewElement_SetsBoth() {
            var guard = Npc("guard_1", new Vector2Int(3, 4));
            var patrol = AddPatrol(guard, Vector2Int.zero, new Vector2Int(0, 3));

            // A new element starts as a copy of the last one.
            var plan = Evaluate(
                "set guard_1 PatrolAIBehaviour.points.size from 2 to 3\n" +
                "set guard_1 PatrolAIBehaviour.points[2].tile from 0 3 to 2 3\n");
            Assert.IsTrue(plan.hasChanges, string.Join("\n", plan.results.ConvertAll(r => r.message)));
            FeatureEditApplier.Commit(plan);

            Assert.AreEqual("3", Field(patrol, "points.Array.size"));
            Assert.AreEqual("2 3", Field(patrol, "points.Array.data[2].tile"));
        }
    }
}
