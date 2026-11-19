using System.Collections.Generic;
using UnityEngine;

namespace GridWorld {
    /// <summary>A parsed map spec. Folders are null when the spec doesn't set them.</summary>
    public class MapSpec {
        public string source = "";
        public string id;
        public string world;
        public string legend;
        public RectInt bounds;
        public string sceneFolder;
        public string dataFolder;
        public List<MapSpecLayer> layers = new List<MapSpecLayer>();
    }
}
