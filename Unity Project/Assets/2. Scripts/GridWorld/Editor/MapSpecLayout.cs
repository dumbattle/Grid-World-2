using System;
using System.Collections.Generic;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Turns a checked spec into the tiles to paint. The top row of a grid is the highest y. Each cell's
    /// logic tile comes from the topmost legend layer whose entry covers it with a footprint, so a tree
    /// over grass uses the tree's. Footprint cells outside the bounds are dropped.
    /// </summary>
    public static class MapSpecLayout {
        public static List<MapTilePlacement> Layout(MapSpec spec, MapLegend legend) {
            var symbol2entry = MapSpecParser.SymbolLookup(legend);
            var placements = new List<MapTilePlacement>();
            var cell2logic = new Dictionary<Vector2Int, (int order, string tile)>();

            foreach (var layer in spec.layers) {
                int order = Array.FindIndex(legend.layers, l => l.name == layer.name);

                for (int r = 0; r < layer.rows.Count; r++) {
                    var row = layer.rows[r];
                    for (int c = 0; c < row.Length; c++) {
                        if (row[c] == MapSpecParser.Empty || !symbol2entry.TryGetValue(row[c], out var entry)) {
                            continue;
                        }

                        var cell = new Vector2Int(spec.bounds.x + c, spec.bounds.yMax - 1 - r);
                        placements.Add(new MapTilePlacement(layer.name, cell, entry.tile));

                        if (entry.hasLogic) {
                            AddLogic(cell2logic, spec.bounds, Footprint(entry, cell), order, entry.logic);
                        }
                    }
                }
            }

            foreach (var pair in cell2logic) {
                placements.Add(new MapTilePlacement(MapSpecParser.LogicLayer, pair.Key, pair.Value.tile));
            }
            return placements;
        }

        static RectInt Footprint(MapLegendEntry entry, Vector2Int cell) {
            var footprint = entry.footprint;
            if (footprint == null || !footprint.isSet) {
                return new RectInt(cell, Vector2Int.one);
            }
            return new RectInt(cell.x + footprint.x, cell.y + footprint.y, footprint.width, footprint.height);
        }

        static void AddLogic(Dictionary<Vector2Int, (int order, string tile)> cell2logic, RectInt bounds, RectInt footprint, int order, string tile) {
            foreach (var cell in footprint.allPositionsWithin) {
                // Logic outside the area's bounds wouldn't bake, and could land in a neighbour's tiles.
                if (!bounds.Contains(cell)) {
                    continue;
                }
                if (!cell2logic.TryGetValue(cell, out var current) || order > current.order) {
                    cell2logic[cell] = (order, tile);
                }
            }
        }
    }
}
