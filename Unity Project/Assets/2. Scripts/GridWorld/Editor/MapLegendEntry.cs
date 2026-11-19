using System;

namespace GridWorld {
    /// <summary>
    /// One character in a legend: the tile it paints on its layer, and optionally the logic tile it
    /// puts under itself, across <see cref="footprint"/> when the art covers more than one cell. Tiles
    /// are asset paths.
    /// </summary>
    [Serializable]
    public class MapLegendEntry {
        public string symbol;
        public string layer;
        public string tile;
        public string logic;
        public MapFootprint footprint = new MapFootprint();

        public bool hasLogic => !string.IsNullOrEmpty(logic);
    }
}
