using System.Collections.Generic;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// A parsed map spec. Folders are null when the spec doesn't set them. With a region, the grids
    /// cover only that part of the bounds, which only applying to an existing area allows.
    /// </summary>
    public class MapSpec {
        public string source = "";
        public string id;
        public string world;
        public string legend;
        public RectInt bounds;
        public bool hasRegion;
        public RectInt region;
        public string sceneFolder;
        public string dataFolder;
        public List<MapSpecLayer> layers = new List<MapSpecLayer>();

        /// <summary>The cells the grids cover: the region if there is one, otherwise the bounds.</summary>
        public RectInt grid => hasRegion ? region : bounds;
    }
}
