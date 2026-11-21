using System.Collections.Generic;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// An existing area's tiles as asset paths: each art tilemap by name, and the Logic tilemap
    /// separately. A path is null for a tile with no asset.
    /// </summary>
    public class MapAreaSnapshot {
        public Dictionary<string, Dictionary<Vector2Int, string>> layer2cells = new Dictionary<string, Dictionary<Vector2Int, string>>();
        public Dictionary<Vector2Int, string> logic = new Dictionary<Vector2Int, string>();

        public Dictionary<Vector2Int, string> Layer(string name) {
            if (!layer2cells.TryGetValue(name, out var cells)) {
                cells = new Dictionary<Vector2Int, string>();
                layer2cells.Add(name, cells);
            }
            return cells;
        }

        public bool HasArt(Vector2Int cell) {
            foreach (var cells in layer2cells.Values) {
                if (cells.ContainsKey(cell)) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>A copy of the art layers, without the logic.</summary>
        public MapAreaSnapshot CopyArt() {
            var copy = new MapAreaSnapshot();
            foreach (var pair in layer2cells) {
                copy.layer2cells.Add(pair.Key, new Dictionary<Vector2Int, string>(pair.Value));
            }
            return copy;
        }
    }
}
