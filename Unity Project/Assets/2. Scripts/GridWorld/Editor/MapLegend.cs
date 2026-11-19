using System;

namespace GridWorld {
    /// <summary>
    /// A theme's characters for map specs, read from a JSON file in <c>Maps/legends/</c>. Layers are
    /// listed bottom to top, which decides which entry's logic tile wins.
    /// </summary>
    [Serializable]
    public class MapLegend {
        public MapLegendLayer[] layers = new MapLegendLayer[0];
        public MapLegendEntry[] entries = new MapLegendEntry[0];
    }
}
