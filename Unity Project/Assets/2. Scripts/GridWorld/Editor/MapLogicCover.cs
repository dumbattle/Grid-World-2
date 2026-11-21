using System.Collections.Generic;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Works out each cell's logic tile from the legend entries painted around it. The entry on the
    /// topmost legend layer wins, across its footprint. Cells outside the bounds are dropped.
    /// </summary>
    internal class MapLogicCover {
        readonly RectInt bounds;
        readonly Dictionary<Vector2Int, (int order, string tile)> cell2logic = new Dictionary<Vector2Int, (int order, string tile)>();

        public IEnumerable<KeyValuePair<Vector2Int, (int order, string tile)>> cells => cell2logic;

        public MapLogicCover(RectInt bounds) {
            this.bounds = bounds;
        }

        /// <summary>Adds an entry painted at a cell on the legend layer with index <paramref name="order"/>.</summary>
        public void Add(MapLegendEntry entry, Vector2Int cell, int order) {
            if (!entry.hasLogic) {
                return;
            }
            foreach (var covered in Footprint(entry, cell).allPositionsWithin) {
                // Logic outside the area's bounds wouldn't bake, and could land in a neighbour's tiles.
                if (!bounds.Contains(covered)) {
                    continue;
                }
                if (!cell2logic.TryGetValue(covered, out var current) || order > current.order) {
                    cell2logic[covered] = (order, entry.logic);
                }
            }
        }

        /// <summary>The logic tile for a cell, or null when no entry with logic covers it.</summary>
        public string Get(Vector2Int cell) {
            return cell2logic.TryGetValue(cell, out var logic) ? logic.tile : null;
        }

        public static RectInt Footprint(MapLegendEntry entry, Vector2Int cell) {
            var footprint = entry.footprint;
            if (footprint == null || !footprint.isSet) {
                return new RectInt(cell, Vector2Int.one);
            }
            return new RectInt(cell.x + footprint.x, cell.y + footprint.y, footprint.width, footprint.height);
        }
    }
}
