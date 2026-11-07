using System;
using LPE;
using NUnit.Framework;
using UnityEngine;

namespace GridWorld.Tests {
    public class EntityComponentManagerTests {
        EntityComponentManager manager;
        EntityID id;

        [SetUp]
        public void SetUp() {
            manager = new EntityComponentManager();
            id = new EntityID(0);
        }

        [Test]
        public void Register_FrameworkStores_AddValues() {
            manager.Register(manager.tile, id, new Vector2Int(3, 4));
            manager.Register(manager.facing, id, Direction4.left);

            Assert.AreEqual(new Vector2Int(3, 4), manager.tile.Get(id));
            Assert.AreSame(Direction4.left, manager.facing.Get(id));
        }

        [Test]
        public void Register_DefinitionStore_KeepsSameAsset() {
            var definition = ScriptableObject.CreateInstance<EntityDefinitionSO>();

            try {
                manager.Register(manager.definition, id, definition);

                Assert.AreSame(definition, manager.definition.Get(id));
            }
            finally {
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void Register_StoreOwnedOutsideManager_AddsValue() {
            var gameStore = new EntityComponent<string>();

            manager.Register(gameStore, id, "value");

            Assert.AreEqual("value", gameStore.Get(id));
        }

        [Test]
        public void Register_ComponentAlreadyPresent_ThrowsAndKeepsOriginal() {
            manager.Register(manager.tile, id, new Vector2Int(1, 1));

            Assert.Throws<ArgumentException>(() => {
                manager.Register(manager.tile, id, new Vector2Int(2, 2));
            });

            Assert.AreEqual(new Vector2Int(1, 1), manager.tile.Get(id));
        }

        [Test]
        public void Register_DifferentEntities_KeepSeparateValues() {
            var other = new EntityID(1);

            manager.Register(manager.tile, id, new Vector2Int(1, 1));
            manager.Register(manager.tile, other, new Vector2Int(2, 2));

            Assert.AreEqual(new Vector2Int(1, 1), manager.tile.Get(id));
            Assert.AreEqual(new Vector2Int(2, 2), manager.tile.Get(other));
        }
    }
}
