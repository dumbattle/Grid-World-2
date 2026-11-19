using UnityEngine;

namespace GridWorld {
    /// <summary>One tile to paint: the tilemap's name, the cell, and the tile's asset path.</summary>
    public struct MapTilePlacement {
        public string layer;
        public Vector2Int cell;
        public string tilePath;

        public MapTilePlacement(string layer, Vector2Int cell, string tilePath) {
            this.layer = layer;
            this.cell = cell;
            this.tilePath = tilePath;
        }
    }
}
