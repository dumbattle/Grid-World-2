using NUnit.Framework;
using UnityEngine.Tilemaps;

namespace GridWorld.Tests {
    public class TilePropertyTableSOTests {
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
        public void TryGetValue_ListedTile_ReturnsEntryBitsAndGameBits() {
            var table = assets.Create<TilePropertyTableSO>();
            var tile = assets.Create<Tile>();
            ushort value = TileBits.EnterUp | TileBits.EnterLeft | 0x2A00;

            table.SetEntry(tile, value);

            Assert.IsTrue(table.TryGetValue(tile, out var found));
            Assert.AreEqual(value, found);
        }

        [Test]
        public void TryGetValue_UnlistedTile_ReturnsFalse() {
            var table = assets.Create<TilePropertyTableSO>();

            Assert.IsFalse(table.TryGetValue(assets.Create<Tile>(), out _));
        }

        [Test]
        public void SetEntry_AfterLookup_ReplacesValue() {
            var table = assets.Create<TilePropertyTableSO>();
            var tile = assets.Create<Tile>();
            table.SetEntry(tile, TileBits.EnterAll);
            table.TryGetValue(tile, out _);

            table.SetEntry(tile, 0);

            Assert.IsTrue(table.TryGetValue(tile, out var found));
            Assert.AreEqual(0, found);
        }
    }
}
