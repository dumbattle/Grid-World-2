using System;

namespace GridWorld {
    /// <summary>A tilemap a legend paints, by GameObject name, and the sorting layer it uses.</summary>
    [Serializable]
    public class MapLegendLayer {
        public string name;
        public string sortingLayer;
    }
}
