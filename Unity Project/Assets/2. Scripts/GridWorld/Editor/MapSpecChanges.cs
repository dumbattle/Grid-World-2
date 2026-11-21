using System.Collections.Generic;

namespace GridWorld {
    /// <summary>
    /// The tiles applying a spec would change. A change with a null tile path clears the cell.
    /// Warnings name cells whose logic was kept rather than worked out from the legend.
    /// </summary>
    public class MapSpecChanges {
        public List<MapTilePlacement> changes = new List<MapTilePlacement>();
        public List<string> warnings = new List<string>();

        public int CountOn(string layer) {
            return changes.FindAll(c => c.layer == layer).Count;
        }
    }
}
