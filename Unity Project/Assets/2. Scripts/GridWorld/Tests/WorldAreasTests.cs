using System;
using NUnit.Framework;
using UnityEngine;

namespace GridWorld.Tests {
    public class WorldAreasTests {
        [Test]
        public void Add_NewArea_StartsInactive() {
            var areas = new WorldAreas();

            areas.Add("a", new RectInt(0, 0, 4, 4));

            Assert.AreEqual(1, areas.count);
            Assert.AreEqual("a", areas.GetId(0));
            Assert.AreEqual(new RectInt(0, 0, 4, 4), areas.GetBounds(0));
            Assert.IsFalse(areas.IsActive(0));
        }

        [Test]
        public void Add_DuplicateId_Throws() {
            var areas = new WorldAreas();
            areas.Add("a", new RectInt(0, 0, 4, 4));

            Assert.Throws<InvalidOperationException>(() => areas.Add("a", new RectInt(4, 0, 4, 4)));
        }

        [Test]
        public void SetupAddArea_TestWorld_AddsTheAreaToo() {
            using (var test = new TestWorld()) {
                Assert.AreEqual(1, test.world.areas.count);
                Assert.AreEqual(TestWorld.AreaBounds, test.world.areas.GetBounds(0));
            }
        }
    }
}
