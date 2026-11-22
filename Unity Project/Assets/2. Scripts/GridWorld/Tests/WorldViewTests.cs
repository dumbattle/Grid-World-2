using LPE;
using NUnit.Framework;
using UnityEngine;

namespace GridWorld.Tests {
    public class WorldViewTests {
        // A second open area just east of the test area, at area index 1.
        static readonly RectInt EastBounds = new RectInt(8, -8, 16, 16);
        const int EastArea = 1;

        TestWorld test;
        RenderContext render;
        GameObject parent;
        WorldView view;

        [SetUp]
        public void SetUp() {
            test = new TestWorld();
            var tiles = new ushort[EastBounds.width * EastBounds.height];
            for (int i = 0; i < tiles.Length; i++) {
                tiles[i] = TileBits.EnterAll;
            }
            test.world.GetContext<WorldSetupContext>().AddArea("east", new AreaTiles(EastBounds, tiles));
            test.SetAreaActive(true);

            render = test.world.GetContext<RenderContext>();
            parent = new GameObject("View");
            view = new WorldView(parent.transform);
        }

        [TearDown]
        public void TearDown() {
            view.Dispose();
            Object.DestroyImmediate(parent);
            test.Dispose();
        }

        void SetEastActive(bool active) {
            test.world.GetContext<AreaActivationContext>().SetAreaActive(EastArea, active);
        }

        int ActiveChildCount() {
            return parent.GetComponentsInChildren<SpriteRenderer>().Length;
        }

        [Test]
        public void Tick_EntityInInactiveArea_HasNoSpriteUntilItActivates() {
            test.Spawn(new Vector2Int(10, 0), Direction4.down);

            view.Tick(render);
            Assert.AreEqual(0, view.visibleCount);
            Assert.AreEqual(0, parent.transform.childCount);

            SetEastActive(true);
            view.Tick(render);
            Assert.AreEqual(1, view.visibleCount);
            Assert.AreEqual(1, ActiveChildCount());
        }

        [Test]
        public void Tick_AreaDeactivated_SpriteIsPooledThenReused() {
            SetEastActive(true);
            test.Spawn(new Vector2Int(10, 0), Direction4.down);
            view.Tick(render);
            var shown = parent.GetComponentInChildren<SpriteRenderer>();

            SetEastActive(false);
            view.Tick(render);
            Assert.AreEqual(0, view.visibleCount);
            Assert.AreEqual(1, view.pooledCount);
            Assert.IsFalse(shown.gameObject.activeSelf);

            SetEastActive(true);
            view.Tick(render);
            Assert.AreEqual(1, parent.transform.childCount);
            Assert.AreSame(shown, parent.GetComponentInChildren<SpriteRenderer>());
        }

        [Test]
        public void Tick_Despawned_SpriteIsReusedByTheNextSpawn() {
            var first = test.Spawn(Vector2Int.zero, Direction4.down);
            view.Tick(render);

            test.Despawn(first);
            view.Tick(render);
            Assert.AreEqual(0, view.visibleCount);
            Assert.AreEqual(1, view.pooledCount);

            test.Spawn(Vector2Int.one, Direction4.up);
            view.Tick(render);
            Assert.AreEqual(1, view.visibleCount);
            Assert.AreEqual(1, parent.transform.childCount);
        }

        [Test]
        public void Tick_StepIntoInactiveArea_VisibleUntilTheStepEnds() {
            var id = test.Spawn(new Vector2Int(7, 0), Direction4.right);
            test.Request(id, Direction4.right);

            // A 16-tick step finishes on tick 16 and clears to idle on tick 17.
            for (int tick = 1; tick <= 16; tick++) {
                test.Tick();
                view.Tick(render);
                Assert.AreEqual(1, view.visibleCount, $"Hidden on tick {tick}.");
            }

            test.Tick();
            view.Tick(render);
            Assert.AreEqual(new Vector2Int(8, 0), test.TileOf(id));
            Assert.AreEqual(0, view.visibleCount);
        }

        [Test]
        public void Tick_StepOutOfInactiveArea_VisibleFromTheStart() {
            var id = test.Spawn(new Vector2Int(8, 0), Direction4.left);
            view.Tick(render);
            Assert.AreEqual(0, view.visibleCount);

            test.Request(id, Direction4.left);
            test.Tick();
            view.Tick(render);

            Assert.AreEqual(1, view.visibleCount);
        }

        [Test]
        public void Dispose_PooledAndShownSprites_AreDestroyed() {
            var pooled = test.Spawn(Vector2Int.zero, Direction4.down);
            test.Spawn(Vector2Int.one, Direction4.down);
            view.Tick(render);
            test.Despawn(pooled);
            view.Tick(render);
            Assert.AreEqual(2, parent.transform.childCount);

            view.Dispose();

            Assert.AreEqual(0, parent.transform.childCount);
        }
    }
}
