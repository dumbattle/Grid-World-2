using System;
using LPE;
using NUnit.Framework;
using UnityEngine;

namespace GridWorld.Tests {
    public class TileOccupancyTests {
        static readonly Vector2Int tile = new Vector2Int(2, 3);

        [Test]
        public void Get_FreeTile_ReturnsInvalid() {
            Assert.IsFalse(new TileOccupancy().Get(tile).IsValid());
        }

        [Test]
        public void Claim_TileHeldByAnother_Throws() {
            var occupancy = new TileOccupancy();
            occupancy.Claim(tile, new EntityID(0));

            Assert.Throws<InvalidOperationException>(() => {
                occupancy.Claim(tile, new EntityID(1));
            });
            Assert.AreEqual(new EntityID(0), occupancy.Get(tile));
        }

        [Test]
        public void Release_ByAnotherEntity_KeepsOccupant() {
            var occupancy = new TileOccupancy();
            occupancy.Claim(tile, new EntityID(0));

            occupancy.Release(tile, new EntityID(1));

            Assert.AreEqual(new EntityID(0), occupancy.Get(tile));
        }

        [Test]
        public void Release_ByOccupant_FreesTile() {
            var occupancy = new TileOccupancy();
            occupancy.Claim(tile, new EntityID(0));

            occupancy.Release(tile, new EntityID(0));

            Assert.IsFalse(occupancy.Get(tile).IsValid());
        }
    }
}
