using NUnit.Framework;
using UnityEngine;

namespace GridWorld.Tests {
    // Default settings: a 15x11 view, so 7.5 tiles to each side, plus margins of 4 to activate and 8
    // to deactivate. Area A spans x 0 to 24 and area B spans x 24 to 48.
    public class AreaActivationSystemTests {
        WorldSettingsSO settings;
        WorldInstance world;
        AreaActivationContext ctx;
        AreaActivationSystem system;

        const int A = 0;
        const int B = 1;

        [SetUp]
        public void SetUp() {
            settings = ScriptableObject.CreateInstance<WorldSettingsSO>();
            world = new WorldInstance();
            world.areas.Add("a", new RectInt(0, 0, 24, 16));
            world.areas.Add("b", new RectInt(24, 0, 24, 16));
            ctx = world.GetContext<AreaActivationContext>();
            system = new AreaActivationSystem(settings);
        }

        [TearDown]
        public void TearDown() {
            Object.DestroyImmediate(settings);
        }

        void RunAt(float x) {
            system.Run(ctx, new Vector2(x, 8));
        }

        [Test]
        public void Run_TargetInsideArea_ActivatesIt() {
            RunAt(5.5f);

            Assert.IsTrue(ctx.IsAreaActive(A));
        }

        [Test]
        public void Run_AreaBeyondActivateMargin_StaysInactive() {
            // The view plus margin reaches x 17.
            RunAt(5.5f);

            Assert.IsFalse(ctx.IsAreaActive(B));
        }

        [Test]
        public void Run_AreaWithinActivateMargin_ActivatesIt() {
            // The view plus margin reaches x 24.5.
            RunAt(13f);

            Assert.IsTrue(ctx.IsAreaActive(B));
        }

        [Test]
        public void Run_BetweenMargins_StaysActive() {
            RunAt(13f);

            // Too far to activate, but not far enough to deactivate.
            RunAt(10f);

            Assert.IsTrue(ctx.IsAreaActive(B));
        }

        [Test]
        public void Run_BeyondDeactivateMargin_DeactivatesIt() {
            RunAt(13f);

            // The view plus the deactivate margin reaches x 23.5.
            RunAt(8f);

            Assert.IsFalse(ctx.IsAreaActive(B));
        }

        [Test]
        public void Run_SteppingBackAndForthAcrossBorder_KeepsBothActive() {
            float[] path = { 23.5f, 24.5f, 23.5f, 24.5f, 23.5f };

            foreach (var x in path) {
                RunAt(x);

                Assert.IsTrue(ctx.IsAreaActive(A), $"A at x {x}");
                Assert.IsTrue(ctx.IsAreaActive(B), $"B at x {x}");
            }
        }

        [Test]
        public void Run_FarAway_DeactivatesTheOldArea() {
            RunAt(5.5f);
            RunAt(45.5f);

            Assert.IsFalse(ctx.IsAreaActive(A));
            Assert.IsTrue(ctx.IsAreaActive(B));
        }
    }
}
