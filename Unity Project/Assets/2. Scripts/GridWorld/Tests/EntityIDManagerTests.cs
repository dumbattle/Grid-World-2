using System.Collections.Generic;
using LPE;
using NUnit.Framework;

namespace GridWorld.Tests {
    public class EntityIDManagerTests {
        [Test]
        public void Create_ManyCalls_ReturnsUniqueValidIds() {
            var manager = new EntityIDManager();
            var seen = new HashSet<EntityID>();

            for (int i = 0; i < 100; i++) {
                var id = manager.Create();
                Assert.IsTrue(id.IsValid());
                Assert.IsTrue(seen.Add(id), $"{id} was returned twice.");
            }

            Assert.AreEqual(100, manager.count);
        }

        [Test]
        public void Create_SeparateManagers_AllocateIndependently() {
            var first = new EntityIDManager();
            var second = new EntityIDManager();

            first.Create();

            Assert.AreEqual(1, first.count);
            Assert.AreEqual(0, second.count);
        }
    }
}
