using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Compares a checked spec with an existing area and lists only the cells that differ. '?' keeps a
    /// cell and '.' clears it. Logic is worked out again only around changed cells, across the
    /// footprints of the entries removed and added there. Within those cells:
    /// <list type="bullet">
    /// <item>A cell no entry with logic covers keeps its logic if it still has art, and is cleared if not.</item>
    /// <item>A cell whose logic differed from the legend's before the edit was hand-painted, so it's kept, with a warning. A cell the legend gave no logic before takes the new logic.</item>
    /// </list>
    /// </summary>
    public static class MapSpecDiff {
        public static MapSpecChanges Compute(MapSpec spec, MapLegend legend, MapAreaSnapshot before) {
            var result = new MapSpecChanges();
            var symbol2entry = MapSpecParser.SymbolLookup(legend);
            var layerTile2entry = new Dictionary<(string layer, string tile), MapLegendEntry>();
            foreach (var entry in legend.entries) {
                if (!string.IsNullOrEmpty(entry.tile) && !layerTile2entry.ContainsKey((entry.layer, entry.tile))) {
                    layerTile2entry.Add((entry.layer, entry.tile), entry);
                }
            }

            var after = before.CopyArt();
            var affected = new HashSet<Vector2Int>();

            foreach (var layer in spec.layers) {
                var cells = after.Layer(layer.name);
                for (int r = 0; r < layer.rows.Count; r++) {
                    var row = layer.rows[r];
                    for (int c = 0; c < row.Length; c++) {
                        char symbol = row[c];
                        string newTile = null;
                        if (symbol == MapSpecParser.Keep) {
                            continue;
                        }
                        if (symbol != MapSpecParser.Empty) {
                            if (!symbol2entry.TryGetValue(symbol, out var entry)) {
                                continue;
                            }
                            newTile = entry.tile;
                        }

                        var cell = MapSpecLayout.Cell(spec, r, c);
                        bool had = cells.TryGetValue(cell, out var oldTile);
                        // A tile with no asset has a null path, so '.' still clears it.
                        if (had ? newTile != null && oldTile == newTile : newTile == null) {
                            continue;
                        }

                        if (newTile == null) {
                            cells.Remove(cell);
                        }
                        else {
                            cells[cell] = newTile;
                        }
                        result.changes.Add(new MapTilePlacement(layer.name, cell, newTile));

                        affected.Add(cell);
                        AddFootprint(affected, layerTile2entry, layer.name, oldTile, cell);
                        AddFootprint(affected, layerTile2entry, layer.name, newTile, cell);
                    }
                }
            }

            if (affected.Count == 0) {
                return result;
            }

            var coverBefore = Cover(before, legend, layerTile2entry, spec.bounds);
            var coverAfter = Cover(after, legend, layerTile2entry, spec.bounds);
            var ordered = new List<Vector2Int>(affected);
            // Top row first, like the grids, so warnings read in spec order.
            ordered.Sort((a, b) => a.y != b.y ? b.y.CompareTo(a.y) : a.x.CompareTo(b.x));

            foreach (var cell in ordered) {
                if (!spec.bounds.Contains(cell)) {
                    continue;
                }
                before.logic.TryGetValue(cell, out var current);
                var expected = coverBefore.Get(cell);
                var target = coverAfter.Get(cell);

                if (target == null) {
                    if (after.HasArt(cell)) {
                        if (current != null) {
                            result.warnings.Add($"({cell.x}, {cell.y}): no legend entry here gives logic, so it keeps {Name(current)}.");
                        }
                        target = current;
                    }
                }
                else if (expected != null && current != expected && target != current) {
                    result.warnings.Add($"({cell.x}, {cell.y}): kept hand-painted logic {Name(current)}, where the legend gives {Name(target)}.");
                    target = current;
                }

                if (target != current) {
                    result.changes.Add(new MapTilePlacement(MapSpecParser.LogicLayer, cell, target));
                }
            }
            return result;
        }

        static void AddFootprint(HashSet<Vector2Int> affected, Dictionary<(string, string), MapLegendEntry> layerTile2entry, string layer, string tile, Vector2Int cell) {
            if (tile == null || !layerTile2entry.TryGetValue((layer, tile), out var entry) || !entry.hasLogic) {
                return;
            }
            foreach (var covered in MapLogicCover.Footprint(entry, cell).allPositionsWithin) {
                affected.Add(covered);
            }
        }

        static MapLogicCover Cover(MapAreaSnapshot art, MapLegend legend, Dictionary<(string, string), MapLegendEntry> layerTile2entry, RectInt bounds) {
            var cover = new MapLogicCover(bounds);
            for (int order = 0; order < legend.layers.Length; order++) {
                var name = legend.layers[order].name;
                if (!art.layer2cells.TryGetValue(name, out var cells)) {
                    continue;
                }
                foreach (var pair in cells) {
                    if (pair.Value != null && layerTile2entry.TryGetValue((name, pair.Value), out var entry)) {
                        cover.Add(entry, pair.Key, order);
                    }
                }
            }
            return cover;
        }

        static string Name(string path) {
            return path == null ? "(none)" : Path.GetFileNameWithoutExtension(path);
        }
    }
}
