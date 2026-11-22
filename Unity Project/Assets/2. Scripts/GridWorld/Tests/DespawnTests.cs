using System;
using LPE;
using NUnit.Framework;
using UnityEngine;

namespace GridWorld.Tests {
    public class DespawnTests {
        TestWorld test;

        [SetUp]
        public void SetUp() {
            test = new TestWorld();
        }

        [TearDown]
        public void TearDown() {
            test.Dispose();
        }

        [Test]
        public void Despawn_EntityWithAI_RemovedFromEveryStore() {
            var id = test.Spawn(Vector2Int.zero, Direction4.down);
            test.AddAI(id, new SpinAI(10, 20));
            var components = test.world.components;

            test.Despawn(id);

            Assert.IsFalse(test.Exists(id));
            Assert.IsFalse(components.tile.Has(id));
            Assert.IsFalse(components.facing.Has(id));
            Assert.IsFalse(components.definition.Has(id));
            Assert.IsFalse(components.step.Has(id));
            Assert.IsFalse(components.ai.Has(id));
        }

        [Test]
        public void Despawn_Entity_FreesItsTile() {
            var id = test.Spawn(Vector2Int.zero, Direction4.down);

            test.Despawn(id);

            Assert.IsFalse(test.world.occupancy.Get(Vector2Int.zero).IsValid());
            Assert.DoesNotThrow(() => test.Spawn(Vector2Int.zero, Direction4.down));
        }

        [Test]
        public void Despawn_MidStep_FreesTheTargetTile() {
            var id = test.Spawn(Vector2Int.zero, Direction4.right);
            test.Request(id, Direction4.right);
            test.Tick();

            test.Despawn(id);

            Assert.IsFalse(test.world.occupancy.Get(Vector2Int.zero).IsValid());
            Assert.IsFalse(test.world.occupancy.Get(Vector2Int.right).IsValid());
        }

        [Test]
        public void Despawn_PendingMoveRequest_IsDropped() {
            var id = test.Spawn(Vector2Int.zero, Direction4.right);
            test.Request(id, Direction4.right);

            test.Despawn(id);

            Assert.IsFalse(test.world.id2moveRequest.ContainsKey(id));
            Assert.DoesNotThrow(() => test.Tick());
        }

        [Test]
        public void Despawn_GameStore_RemovedToo() {
            var id = test.Spawn(Vector2Int.zero, Direction4.down);
            var health = new EntityComponent<int>();
            var components = test.world.components;
            components.AddStore(health);
            components.Register(health, id, 10);

            test.Despawn(id);

            Assert.IsFalse(health.Has(id));
        }

        [Test]
        public void AddStore_Twice_Throws() {
            var store = new EntityComponent<int>();
            test.world.components.AddStore(store);

            Assert.Throws<InvalidOperationException>(() => test.world.components.AddStore(store));
        }

        [Test]
        public void Despawn_Player_Throws() {
            var player = test.SpawnPlayer(Vector2Int.zero, Direction4.down);

            Assert.Throws<InvalidOperationException>(() => test.Despawn(player));
            Assert.IsTrue(test.Exists(player));
        }

        [Test]
        public void Despawn_Twice_Throws() {
            var id = test.Spawn(Vector2Int.zero, Direction4.down);
            test.Despawn(id);

            Assert.Throws<InvalidOperationException>(() => test.Despawn(id));
        }

        [Test]
        public void Spawn_AfterDespawn_GetsANewID() {
            var first = test.Spawn(Vector2Int.zero, Direction4.down);
            test.Despawn(first);

            var second = test.Spawn(Vector2Int.zero, Direction4.down);

            Assert.AreNotEqual(first, second);
            Assert.IsFalse(test.Exists(first));
            Assert.AreEqual(1, test.world.entityIds.liveCount);
        }

        [Test]
        public void WorldView_Despawned_SpriteIsDestroyed() {
            var parent = new GameObject("View");
            var view = new WorldView(parent.transform);
            var render = test.world.GetContext<RenderContext>();
            try {
                var id = test.Spawn(Vector2Int.zero, Direction4.down);
                view.Tick(render);
                Assert.AreEqual(1, parent.transform.childCount);

                test.Despawn(id);
                view.Tick(render);

                Assert.AreEqual(0, parent.transform.childCount);
            }
            finally {
                view.Dispose();
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }
    }
}
