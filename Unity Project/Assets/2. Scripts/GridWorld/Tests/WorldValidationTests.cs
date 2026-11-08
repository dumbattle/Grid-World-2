using NUnit.Framework;
using UnityEngine;

namespace GridWorld.Tests {
    public class WorldValidationTests {
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
        public void FindError_TouchingAreas_ReturnsNull() {
            var a = assets.Area("a", new RectInt(0, 0, 4, 4), Vector2Int.zero);
            var b = assets.Area("b", new RectInt(4, 0, 4, 4), Vector2Int.zero);

            Assert.IsNull(WorldValidation.FindError(new[] { a, b }));
        }

        [Test]
        public void FindError_OverlappingAreas_NamesBoth() {
            var a = assets.Area("first", new RectInt(0, 0, 4, 4), Vector2Int.zero);
            var b = assets.Area("second", new RectInt(3, 3, 4, 4), Vector2Int.zero);

            var error = WorldValidation.FindError(new[] { a, b });

            StringAssert.Contains("overlap", error);
            StringAssert.Contains("first", error);
            StringAssert.Contains("second", error);
        }

        [Test]
        public void FindError_OffsetMovesAreaOntoAnother_ReportsOverlap() {
            var a = assets.Area("outdoor", new RectInt(0, 0, 8, 8), Vector2Int.zero);
            var b = assets.Area("interior", new RectInt(0, 0, 4, 4), new Vector2Int(6, 6));

            StringAssert.Contains("overlap", WorldValidation.FindError(new[] { a, b }));
        }

        [Test]
        public void FindError_DuplicateAreaId_NamesId() {
            var a = assets.Area("same", new RectInt(0, 0, 4, 4), Vector2Int.zero);
            var b = assets.Area("same", new RectInt(10, 0, 4, 4), Vector2Int.zero);

            StringAssert.Contains("'same'", WorldValidation.FindError(new[] { a, b }));
        }

        [Test]
        public void FindError_MissingArea_ReportsIt() {
            var a = assets.Area("a", new RectInt(0, 0, 4, 4), Vector2Int.zero);

            StringAssert.Contains("missing", WorldValidation.FindError(new[] { a, null }));
        }
    }
}
