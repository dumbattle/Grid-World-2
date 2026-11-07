using System;
using LPE;
using NUnit.Framework;
using UnityEngine;

namespace GridWorld.Tests {
    public class WorldTilesTests {
        static AreaTiles Area(RectInt bounds, ushort fill) {
            var tiles = new ushort[bounds.width * bounds.height];
            for (int i = 0; i < tiles.Length; i++) {
                tiles[i] = fill;
            }
            return new AreaTiles(bounds, tiles);
        }

        [Test]
        public void Get_OutsideEveryArea_ReturnsZero() {
            var tiles = new WorldTiles();
            tiles.AddArea(Area(new RectInt(0, 0, 4, 4), TileBits.EnterAll));

            Assert.AreEqual(0, tiles.Get(new Vector2Int(4, 0)));
            Assert.AreEqual(0, tiles.Get(new Vector2Int(-1, 0)));
            Assert.IsFalse(tiles.CanEnter(new Vector2Int(4, 0), Direction4.right));
        }

        [Test]
        public void CanEnter_UnpaintedTile_IsBlockedFromEveryDirection() {
            var tiles = new WorldTiles();
            tiles.AddArea(Area(new RectInt(0, 0, 4, 4), 0));

            Assert.IsFalse(tiles.CanEnter(new Vector2Int(1, 1), Direction4.up));
            Assert.IsFalse(tiles.CanEnter(new Vector2Int(1, 1), Direction4.down));
            Assert.IsFalse(tiles.CanEnter(new Vector2Int(1, 1), Direction4.left));
            Assert.IsFalse(tiles.CanEnter(new Vector2Int(1, 1), Direction4.right));
        }

        [Test]
        public void CanEnter_OneEntryBit_AllowsOnlyThatDirection() {
            var tiles = new WorldTiles();
            tiles.AddArea(Area(new RectInt(0, 0, 4, 4), TileBits.EnterDown));

            Assert.IsTrue(tiles.CanEnter(new Vector2Int(1, 1), Direction4.down));
            Assert.IsFalse(tiles.CanEnter(new Vector2Int(1, 1), Direction4.up));
            Assert.IsFalse(tiles.CanEnter(new Vector2Int(1, 1), Direction4.left));
            Assert.IsFalse(tiles.CanEnter(new Vector2Int(1, 1), Direction4.right));
        }

        [Test]
        public void Get_TileChange_OverridesArea() {
            var tiles = new WorldTiles();
            tiles.AddArea(Area(new RectInt(0, 0, 4, 4), TileBits.EnterAll));

            tiles.SetChange(new Vector2Int(2, 2), 0);

            Assert.AreEqual(0, tiles.Get(new Vector2Int(2, 2)));
            Assert.AreEqual(TileBits.EnterAll, tiles.Get(new Vector2Int(2, 3)));
        }

        [Test]
        public void Get_GameBits_ArePreserved() {
            var tiles = new WorldTiles();
            ushort value = TileBits.EnterAll | 0x0100;
            tiles.AddArea(Area(new RectInt(0, 0, 4, 4), value));

            Assert.AreEqual(value, tiles.Get(new Vector2Int(0, 0)));
            Assert.IsTrue(tiles.CanEnter(new Vector2Int(0, 0), Direction4.up));
        }

        [Test]
        public void Get_OffsetArea_ReadsLocalIndex() {
            var data = new ushort[3 * 2];
            data[1 + 1 * 3] = TileBits.EnterUp;
            var tiles = new WorldTiles();
            tiles.AddArea(new AreaTiles(new RectInt(10, 20, 3, 2), data));

            Assert.AreEqual(TileBits.EnterUp, tiles.Get(new Vector2Int(11, 21)));
            Assert.AreEqual(0, tiles.Get(new Vector2Int(10, 20)));
        }

        [Test]
        public void Get_TwoAreas_ReadsFromTheAreaHoldingTheTile() {
            var tiles = new WorldTiles();
            tiles.AddArea(Area(new RectInt(0, 0, 4, 4), TileBits.EnterUp));
            tiles.AddArea(Area(new RectInt(4, 0, 4, 4), TileBits.EnterDown));

            Assert.AreEqual(TileBits.EnterUp, tiles.Get(new Vector2Int(3, 0)));
            Assert.AreEqual(TileBits.EnterDown, tiles.Get(new Vector2Int(4, 0)));
        }

        [Test]
        public void AreaTiles_WrongTileCount_Throws() {
            Assert.Throws<ArgumentException>(() => {
                new AreaTiles(new RectInt(0, 0, 4, 4), new ushort[15]);
            });
        }
    }
}
