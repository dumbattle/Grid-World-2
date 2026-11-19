using System;

namespace GridWorld {
    /// <summary>
    /// The cells an entry's logic tile covers, measured from the cell it's painted on. A size of zero
    /// means just that cell.
    /// </summary>
    [Serializable]
    public class MapFootprint {
        public int x;
        public int y;
        public int width;
        public int height;

        public bool isSet => width != 0 || height != 0 || x != 0 || y != 0;
    }
}
