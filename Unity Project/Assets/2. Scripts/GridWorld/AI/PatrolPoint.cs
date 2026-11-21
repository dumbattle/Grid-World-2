using LPE;
using UnityEngine;

namespace GridWorld {
    public readonly struct PatrolPoint {
        public readonly Vector2Int tile;
        // Null keeps whatever way the entity arrived facing.
        public readonly Direction4 facing;
        public readonly int waitTicks;

        public PatrolPoint(Vector2Int tile, Direction4 facing, int waitTicks) {
            this.tile = tile;
            this.facing = facing;
            this.waitTicks = waitTicks;
        }
    }
}
