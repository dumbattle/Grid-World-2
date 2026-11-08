using System.Collections.Generic;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Collects the records that feature behaviours export while their area bakes.
    /// </summary>
    public class WorldFeatureBaker {
        Grid grid;
        List<WorldFeatureData> records;

        public string areaId { get; }
        public bool hasErrors { get; private set; }

        public WorldFeatureBaker(string areaId, Grid grid, List<WorldFeatureData> records) {
            this.areaId = areaId;
            this.grid = grid;
            this.records = records;
        }

        /// <summary>The area-local tile under a world position.</summary>
        public Vector2Int ToLocalTile(Vector3 position) {
            var cell = grid.WorldToCell(position);
            return new Vector2Int(cell.x, cell.y);
        }

        public void Add(WorldFeatureData record) {
            records.Add(record);
        }

        public void LogError(Object context, string message) {
            hasErrors = true;
            Debug.LogError($"Area '{areaId}': {message}", context);
        }
    }
}
