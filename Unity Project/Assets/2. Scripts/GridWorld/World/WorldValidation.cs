using System.Collections.Generic;

namespace GridWorld {
    public static class WorldValidation {
        /// <summary>
        /// Returns a message describing the first missing area, duplicate area ID, pair of
        /// overlapping areas, or duplicate feature ID, or null if the areas are valid. Touching areas
        /// don't overlap.
        /// </summary>
        public static string FindError(IReadOnlyList<AreaDataSO> areas) {
            for (int i = 0; i < areas.Count; i++) {
                var a = areas[i];
                if (a == null) {
                    return $"Area {i} in the world's list is missing.";
                }
                if (string.IsNullOrEmpty(a.areaId)) {
                    return $"Area data '{a.name}' has no area ID. Bake its scene.";
                }

                for (int j = i + 1; j < areas.Count; j++) {
                    var b = areas[j];
                    if (b == null) {
                        continue;
                    }
                    if (a.areaId == b.areaId) {
                        return $"Areas '{a.name}' and '{b.name}' share the area ID '{a.areaId}'.";
                    }
                    if (a.worldBounds.Overlaps(b.worldBounds)) {
                        return $"Areas '{a.areaId}' {a.worldBounds} and '{b.areaId}' {b.worldBounds} overlap.";
                    }
                }
            }
            return FindDuplicateFeatureId(areas);
        }

        // Features without an ID are skipped, and so are records whose class is missing.
        static string FindDuplicateFeatureId(IReadOnlyList<AreaDataSO> areas) {
            var id2area = new Dictionary<string, string>(FeatureIds.comparer);
            for (int i = 0; i < areas.Count; i++) {
                var records = areas[i].featureRecords;
                for (int r = 0; r < records.Count; r++) {
                    if (!(records[r] is IdentifiedFeatureData record) || record.id.Length == 0) {
                        continue;
                    }

                    var areaId = areas[i].areaId;
                    if (id2area.TryGetValue(record.id, out var otherAreaId)) {
                        return otherAreaId == areaId
                            ? $"Area '{areaId}' has two features with the ID '{record.id}'."
                            : $"Areas '{otherAreaId}' and '{areaId}' both have a feature with the ID '{record.id}'.";
                    }
                    id2area.Add(record.id, areaId);
                }
            }
            return null;
        }
    }
}
