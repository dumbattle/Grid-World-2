using UnityEngine;

namespace GridWorld {
    /// <summary>Where a spec's grid cells land in the world. The top row of a grid is the highest y.</summary>
    public static class MapSpecLayout {
        /// <summary>The cell for a grid row and column. Row 0 is the top of the grid, the highest y.</summary>
        public static Vector2Int Cell(MapSpec spec, int row, int column) {
            var grid = spec.grid;
            return new Vector2Int(grid.x + column, grid.yMax - 1 - row);
        }
    }
}
