using System;
using System.Collections.Generic;
using LPE;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Which entity occupies each tile. There's at most one entity per tile.
    /// </summary>
    public class TileOccupancy {
        Dictionary<Vector2Int, EntityID> tile2entity = new Dictionary<Vector2Int, EntityID>();

        public EntityID Get(Vector2Int tile) {
            if (tile2entity.TryGetValue(tile, out var id)) {
                return id;
            }
            return EntityID.INVALID;
        }

        public void Claim(Vector2Int tile, EntityID id) {
            if (tile2entity.TryGetValue(tile, out var existing) && existing != id) {
                throw new InvalidOperationException($"{id} can't claim {tile}: {existing} already occupies it.");
            }
            tile2entity[tile] = id;
        }

        public void Release(Vector2Int tile, EntityID id) {
            if (tile2entity.TryGetValue(tile, out var existing) && existing == id) {
                tile2entity.Remove(tile);
            }
        }
    }
}
