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
            var logic = new MapLogicCover(spec.bounds);

            foreach (var layer in spec.layers) {
                int order = Array.FindIndex(legend.layers, l => l.name == layer.name);

                for (int r = 0; r < layer.rows.Count; r++) {
                    var row = layer.rows[r];
                    for (int c = 0; c < row.Length; c++) {
                        if (!symbol2entry.TryGetValue(row[c], out var entry)) {
                            continue;
                        }

                        var cell = Cell(spec, r, c);
                        placements.Add(new MapTilePlacement(layer.name, cell, entry.tile));
                        logic.Add(entry, cell, order);
                    }
                }
            }

            foreach (var pair in logic.cells) {
                placements.Add(new MapTilePlacement(MapSpecParser.LogicLayer, pair.Key, pair.Value.tile));
            }
            return placements;
        }

        /// <summary>The cell for a grid row and column. Row 0 is the top of the grid, the highest y.</summary>
        public static Vector2Int Cell(MapSpec spec, int row, int column) {
            var grid = spec.grid;
            return new Vector2Int(grid.x + column, grid.yMax - 1 - row);
        }
    }
}
