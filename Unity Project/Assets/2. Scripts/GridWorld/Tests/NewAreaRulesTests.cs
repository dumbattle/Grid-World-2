using LPE;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GridWorld.Tests {
    public class NewAreaRulesTests {
        static readonly RectInt Current = new RectInt(10, 20, 8, 6);

        TestAssets assets;

        [SetUp]
        public void SetUp() {
            assets = new TestAssets();
        }

        [TearDown]
        public void TearDown() {
            assets.Dispose();
        }

        [Test]
        public void NeighbourBounds_Right_TouchesRightEdge() {
            Assert.AreEqual(new RectInt(18, 20, 8, 6), NewAreaRules.NeighbourBounds(Current, Direction4.right));
        }

        [Test]
        public void NeighbourBounds_Left_TouchesLeftEdge() {
            Assert.AreEqual(new RectInt(2, 20, 8, 6), NewAreaRules.NeighbourBounds(Current, Direction4.left));
        }

        [Test]
        public void NeighbourBounds_Up_TouchesTopEdge() {
            Assert.AreEqual(new RectInt(10, 26, 8, 6), NewAreaRules.NeighbourBounds(Current, Direction4.up));
        }

        [Test]
        public void NeighbourBounds_Down_TouchesBottomEdge() {
            Assert.AreEqual(new RectInt(10, 14, 8, 6), NewAreaRules.NeighbourBounds(Current, Direction4.down));
        }

        [Test]
        public void FindError_TakenId_NamesId() {
            var areas = new[] { assets.Area("town", new RectInt(0, 0, 4, 4), Vector2Int.zero) };

            StringAssert.Contains("town", NewAreaRules.FindError("town", new RectInt(10, 0, 4, 4), areas));
        }

        [Test]
        public void FindError_OverlappingBounds_ReportsOverlap() {
            var areas = new[] { assets.Area("town", new RectInt(0, 0, 4, 4), Vector2Int.zero) };

            StringAssert.Contains("overlap", NewAreaRules.FindError("field", new RectInt(3, 0, 4, 4), areas));
        }

        [Test]
        public void FindError_TouchingBounds_ReturnsNull() {
            var areas = new[] { assets.Area("town", new RectInt(0, 0, 4, 4), Vector2Int.zero) };

            Assert.IsNull(NewAreaRules.FindError("field", new RectInt(4, 0, 4, 4), areas));
        }

        [Test]
        public void FindError_EmptyId_ReturnsError() {
            Assert.IsNotNull(NewAreaRules.FindError(" ", new RectInt(0, 0, 4, 4), new AreaDataSO[0]));
        }

        [Test]
        public void FindError_EmptyBounds_ReturnsError() {
            Assert.IsNotNull(NewAreaRules.FindError("field", new RectInt(0, 0, 0, 4), new AreaDataSO[0]));
        }

        [Test]
        public void SceneAndDataName_FromId() {
            Assert.AreEqual("Area_town", NewAreaRules.SceneName("town"));
            Assert.AreEqual("AreaData_town", NewAreaRules.DataName("town"));
        }

        [Test]
        public void DefaultBounds_NoAreas_UsesViewSizeAtOrigin() {
            var settings = assets.Create<WorldSettingsSO>();
            var world = assets.Create<WorldDataSO>();
            var properties = new SerializedObject(world);
            properties.FindProperty("worldSettings").objectReferenceValue = settings;
            properties.ApplyModifiedPropertiesWithoutUndo();

            Assert.AreEqual(new RectInt(Vector2Int.zero, settings.viewTiles), NewAreaRules.DefaultBounds(world));
        }

        [Test]
        public void DefaultBounds_WithAreas_RightOfLastArea() {
            var world = assets.Create<WorldDataSO>();
            world.AddArea(assets.Area("town", new RectInt(0, 0, 4, 3), Vector2Int.zero));

            Assert.AreEqual(new RectInt(4, 0, 4, 3), NewAreaRules.DefaultBounds(world));
        }

        [Test]
        public void DefaultFolder_SkipsEmptyPaths() {
            Assert.AreEqual("Assets/Scenes", NewAreaRules.DefaultFolder(null, "", "Assets/Scenes/Area_town.unity"));
        }

        [Test]
        public void DefaultFolder_NoPaths_ReturnsAssets() {
            Assert.AreEqual("Assets", NewAreaRules.DefaultFolder(null, ""));
        }
    }
}
