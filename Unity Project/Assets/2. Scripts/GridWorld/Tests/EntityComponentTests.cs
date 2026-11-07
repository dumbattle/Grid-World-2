using System.Collections.Generic;
using System.Reflection;
using LPE;
using NUnit.Framework;

namespace GridWorld.Tests {
    public class EntityComponentTests {
        EntityComponentManager manager;
        EntityComponent<int> store;
        EntityID id;

        [SetUp]
        public void SetUp() {
            manager = new EntityComponentManager();
            store = new EntityComponent<int>();
            id = new EntityID(0);
        }

        [Test]
        public void Add_FromOutsideTheFramework_IsNotPublic() {
            var add = typeof(EntityComponent<int>).GetMethod("Add", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.IsTrue(add.IsAssembly);
        }

        [Test]
        public void Has_NoComponent_ReturnsFalse() {
            Assert.IsFalse(store.Has(id));
        }

        [Test]
        public void Get_NoComponent_Throws() {
            Assert.Throws<KeyNotFoundException>(() => {
                store.Get(id);
            });
        }

        [Test]
        public void TryGet_NoComponent_ReturnsFalse() {
            Assert.IsFalse(store.TryGet(id, out _));
        }

        [Test]
        public void TryGet_HasComponent_ReturnsValue() {
            manager.Register(store, id, 5);

            Assert.IsTrue(store.TryGet(id, out var value));
            Assert.AreEqual(5, value);
        }

        [Test]
        public void Set_HasComponent_ReplacesValue() {
            manager.Register(store, id, 5);

            store.Set(id, 9);

            Assert.AreEqual(9, store.Get(id));
            Assert.AreEqual(1, store.count);
        }

        [Test]
        public void Set_NoComponent_ThrowsAndAddsNothing() {
            Assert.Throws<KeyNotFoundException>(() => {
                store.Set(id, 9);
            });

            Assert.IsFalse(store.Has(id));
        }
    }
}
