using System.Collections.Generic;

namespace GridWorld {
    /// <summary>A parsed feature edit file: one area's edits, from <see cref="FeatureEditParser"/>.</summary>
    public class FeatureEditFile {
        public string source;
        public string areaId;
        /// <summary>The world data asset's path, like Assets/8. Data/World.asset.</summary>
        public string world;
        public List<FeatureEdit> edits = new List<FeatureEdit>();
    }
}
