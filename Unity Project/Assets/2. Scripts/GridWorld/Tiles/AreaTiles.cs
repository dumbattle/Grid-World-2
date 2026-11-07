using System;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// One area's tile data, placed in world tiles. An unpainted tile is 0, so it allows no entry.
    /// </summary>
    public class AreaTiles {
        ushort[] tiles;

        public RectInt bounds { get; }

        public AreaTiles(RectInt bounds, ushort[] tiles) {
            if (bounds.width <= 0 || bounds.height <= 0) {
                throw new ArgumentException($"Area bounds {bounds} must have a positive size.", nameof(bounds));
            }
            if (tiles == null || tiles.Length != bounds.width * bounds.height) {
                throw new ArgumentException($"Area bounds {bounds} need {bounds.width * bounds.height} tiles.", nameof(tiles));
            }

            this.bounds = bounds;
            this.tiles = tiles;
        }

        public bool Contains(Vector2Int tile) {
            return bounds.Contains(tile);
        }

        public ushort Get(Vector2Int tile) {
            return tiles[(tile.x - bounds.xMin) + (tile.y - bounds.yMin) * bounds.width];
        }
    }
}
