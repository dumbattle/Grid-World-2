using NUnit.Framework;
using UnityEngine;

namespace GridWorld.Tests {
    // Default settings: a 15x11 view, so 7.5 tiles to each side, plus margins of 4 to activate and 8
    // to deactivate.
    public class AreaActivationSystemTests {
        WorldSettingsSO settings;
        WorldInstance world;
        AreaActivationContext ctx;
        AreaActivationSystem system;

        const int A = 0;
        const int B = 1;
        const int C = 2;

        [SetUp]
        public void SetUp() {
            settings = ScriptableObject.CreateInstance<WorldSettingsSO>();
            world = new WorldInstance();
            ctx = world.GetContext<AreaActivationContext>();
            system = new AreaActivationSystem(settings);
        }

        [TearDown]
        public void TearDown() {
            Object.DestroyImmediate(settings);
        }

        void AddArea(string id, int x, int y, int width, int height) {
            world.areas.Add(id, new RectInt(x, y, width, height));
        }

        // A spans x 0 to 24 and B spans x 24 to 48.
        void AddTouchingPair() {
            AddArea("a", 0, 0, 24, 16);
            AddArea("b", 24, 0, 24, 16);
        }

        // A spans x 0 to 24 and B spans x 25 to 49, so only the camera margins can activate B.
        void AddSeparatedPair() {
            AddArea("a", 0, 0, 24, 16);
            AddArea("b", 25, 0, 24, 16);
        }

        void RunAt(float x, float y = 8f) {
            system.Run(ctx, new Vector2(x, y));
        }

        [Test]
        public void Touches_SharedEdge_ReturnsTrue() {
            Assert.IsTrue(AreaActivationSystem.Touches(new RectInt(0, 0, 4, 4), new RectInt(4, 1, 4, 4)));
        }

        [Test]
        public void Touches_SharedCorner_ReturnsTrue() {
            Assert.IsTrue(AreaActivationSystem.Touches(new RectInt(0, 0, 4, 4), new RectInt(4, 4, 4, 4)));
        }

        [Test]
        public void Touches_OneTileGap_ReturnsFalse() {
            Assert.IsFalse(AreaActivationSystem.Touches(new RectInt(0, 0, 4, 4), new RectInt(5, 0, 4, 4)));
        }

        [Test]
        public void Run_TargetInsideArea_ActivatesIt() {
            AddSeparatedPair();

            RunAt(5.5f);

            Assert.IsTrue(ctx.IsAreaActive(A));
        }

        [Test]
        public void Run_TouchingAreaBeyondMargins_ActivatesIt() {
            AddTouchingPair();

            // The view plus the activate margin reaches only x 17.
            RunAt(5.5f);

            Assert.IsTrue(ctx.IsAreaActive(B));
        }

        [Test]
        public void Run_AreaTouchingOnlyAtCorner_ActivatesIt() {
            AddArea("a", 0, 0, 24, 16);
            AddArea("b", 24, 16, 24, 16);

            RunAt(5.5f, 5.5f);

            Assert.IsTrue(ctx.IsAreaActive(B));
        }

        [Test]
        public void Run_TwoAreasAway_DeactivatesIt() {
            AddArea("a", 0, 0, 24, 16);
            AddArea("b", 24, 0, 24, 16);
            AddArea("c", 48, 0, 24, 16);

            RunAt(30.5f);
            Assert.IsTrue(ctx.IsAreaActive(A));

            // In C, A no longer touches the current area and is far beyond the deactivate margin.
            RunAt(60.5f);

            Assert.IsFalse(ctx.IsAreaActive(A));
            Assert.IsTrue(ctx.IsAreaActive(B));
            Assert.IsTrue(ctx.IsAreaActive(C));
        }

        [Test]
        public void Run_TargetOutsideEveryArea_UsesMarginsOnly() {
            AddSeparatedPair();

            // In the gap: B is within the margin, A is too, and neither is the current area.
            RunAt(24.5f);

            Assert.IsTrue(ctx.IsAreaActive(A));
            Assert.IsTrue(ctx.IsAreaActive(B));

            // Far below both areas, out of every margin.
            RunAt(24.5f, -40f);

            Assert.IsFalse(ctx.IsAreaActive(A));
            Assert.IsFalse(ctx.IsAreaActive(B));
        }

        [Test]
        public void Run_AreaBeyondActivateMargin_StaysInactive() {
            AddSeparatedPair();

            // The view plus margin reaches x 24.5.
            RunAt(13f);

            Assert.IsFalse(ctx.IsAreaActive(B));
        }

        [Test]
        public void Run_AreaWithinActivateMargin_ActivatesIt() {
            AddSeparatedPair();

            // The view plus margin reaches x 25.5.
            RunAt(14f);

            Assert.IsTrue(ctx.IsAreaActive(B));
        }

        [Test]
        public void Run_BetweenMargins_StaysActive() {
            AddSeparatedPair();
            RunAt(14f);

            // The view plus the deactivate margin reaches x 25.5: too far to activate, but not far
            // enough to deactivate.
            RunAt(10f);

            Assert.IsTrue(ctx.IsAreaActive(B));
        }

        [Test]
        public void Run_BeyondDeactivateMargin_DeactivatesIt() {
            AddSeparatedPair();
            RunAt(14f);

            // The view plus the deactivate margin reaches x 24.5.
            RunAt(9f);

            Assert.IsFalse(ctx.IsAreaActive(B));
        }

        [Test]
        public void Run_SteppingBackAndForthAcrossBorder_KeepsBothActive() {
            AddTouchingPair();
            float[] path = { 23.5f, 24.5f, 23.5f, 24.5f, 23.5f };

            foreach (var x in path) {
                RunAt(x);

                Assert.IsTrue(ctx.IsAreaActive(A), $"A at x {x}");
                Assert.IsTrue(ctx.IsAreaActive(B), $"B at x {x}");
            }
        }

        [Test]
        public void Run_FarAway_DeactivatesTheOldArea() {
            AddSeparatedPair();

            RunAt(5.5f);
            RunAt(45.5f);

            Assert.IsFalse(ctx.IsAreaActive(A));
            Assert.IsTrue(ctx.IsAreaActive(B));
        }
    }
}
