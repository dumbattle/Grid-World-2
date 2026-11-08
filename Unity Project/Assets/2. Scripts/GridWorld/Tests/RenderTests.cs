using System.Collections.Generic;
using LPE;
using NUnit.Framework;
using UnityEngine;

namespace GridWorld.Tests {
    public class RenderTests {
        TestWorld test;
        RenderContext render;

        [SetUp]
        public void SetUp() {
            test = new TestWorld();
            render = test.world.GetContext<RenderContext>();
        }

        [TearDown]
        public void TearDown() {
            test.Dispose();
        }

        [Test]
        public void CollectEntities_TwoEntities_ListsBoth() {
            var a = test.Spawn(Vector2Int.zero, Direction4.down);
            var b = test.Spawn(Vector2Int.one, Direction4.up);
            var entities = new List<EntityID>();

            render.CollectEntities(entities);

            CollectionAssert.AreEquivalent(new[] { a, b }, entities);
        }

        [Test]
        public void GetBlendedPosition_Walking_FollowsTheStep() {
            var id = test.Spawn(Vector2Int.zero, Direction4.right);
            test.Request(id, Direction4.right);
            test.Tick(8);

            // 8 of 16 ticks done, so the last two ticks were at 7/16 and 8/16.
            Assert.AreEqual(7.5f / 16f, render.GetBlendedPosition(id, 0.5f).x, 1e-4f);
        }

        [Test]
        public void FollowCamera_Walking_CentresOnTheBlendedPosition() {
            var id = test.SpawnPlayer(Vector2Int.zero, Direction4.right);
            var camera = new FollowCameraController(id);
            test.Request(id, Direction4.right);
            test.Tick(8);

            var view = camera.GetViewPosition(render, 0.5f);

            Assert.AreEqual(7.5f / 16f + 0.5f, view.x, 1e-4f);
            Assert.AreEqual(0.5f, view.y, 1e-4f);
        }

        [Test]
        public void FollowCamera_Walking_TargetIsTheClaimedTile() {
            var id = test.SpawnPlayer(Vector2Int.zero, Direction4.right);
            var camera = new FollowCameraController(id);
            test.Request(id, Direction4.right);
            test.Tick();

            Assert.AreEqual(new Vector2(1.5f, 0.5f), camera.GetTarget(render));
        }

        [Test]
        public void GetCameraController_SetByRoot_ReturnsIt() {
            var camera = new FollowCameraController(EntityID.INVALID);
            test.world.cameraController = camera;

            Assert.AreSame(camera, render.GetCameraController());
        }

        [Test]
        public void WorldView_Tick_CreatesOneSpriteFacingEachWay() {
            var parent = new GameObject("View");
            var view = new WorldView(parent.transform);
            try {
                test.Spawn(Vector2Int.zero, Direction4.down);
                test.Spawn(new Vector2Int(2, 0), Direction4.left);

                view.Tick(render);
                view.Tick(render);
                view.Render(render, 1f);

                var sprites = parent.GetComponentsInChildren<SpriteRenderer>();
                Assert.AreEqual(2, sprites.Length);
                Assert.AreNotEqual(sprites[0].sprite, sprites[1].sprite);
            }
            finally {
                view.Dispose();
                Object.DestroyImmediate(parent);
            }
        }
    }
}
