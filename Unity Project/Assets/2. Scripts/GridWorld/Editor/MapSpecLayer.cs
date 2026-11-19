using System.Collections.Generic;

namespace GridWorld {
    /// <summary>One layer's grid from a map spec, top row first, with each row's line number.</summary>
    public class MapSpecLayer {
        public string name;
        public int line;
        public List<string> rows = new List<string>();
        public List<int> rowLines = new List<int>();
    }
}
