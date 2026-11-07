using System.Collections.Generic;
using LPE;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// The one lookup for tile data: tile changes first, then the area holding the tile. A tile outside
    /// every area is 0, so it allows no entry.
    /// </summary>
    public class WorldTiles {
        List<AreaTiles> areas = new List<AreaTiles>();
        Dictionary<Vector2Int, ushort> tile2change = new Dictionary<Vector2Int, ushort>();

        public void AddArea(AreaTiles area) {
            areas.Add(area);
        }

        public void SetChange(Vector2Int tile, ushort value) {
            tile2change[tile] = value;
        }

        public ushort Get(Vector2Int tile) {
            if (tile2change.TryGetValue(tile, out var changed)) {
                return changed;
            }

            for (int i = 0; i < areas.Count; i++) {
                if (areas[i].Contains(tile)) {
                    return areas[i].Get(tile);
                }
            }
            return 0;
        }

        public bool CanEnter(Vector2Int tile, Direction4 direction) {
            return (Get(tile) & TileBits.EnterBit(direction)) != 0;
        }
    }
}
