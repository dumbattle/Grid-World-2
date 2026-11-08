using System;
using System.Collections.Generic;

namespace GridWorld {
    /// <summary>
    /// Builds world state from baked data: every area's tiles first, then every feature record, so
    /// features can rely on all tile data being in place.
    /// </summary>
    public static class WorldStartup {
        public static void Load(WorldSetupContext ctx, WorldDataSO world, WorldFeatureCatalogue catalogue) {
            if (world == null) {
                throw new ArgumentNullException(nameof(world));
            }
            Load(ctx, world.areas, catalogue);
        }

        public static void Load(WorldSetupContext ctx, IReadOnlyList<AreaDataSO> areas, WorldFeatureCatalogue catalogue) {
            var error = WorldValidation.FindError(areas);
            if (error != null) {
                throw new InvalidOperationException(error);
            }

            for (int i = 0; i < areas.Count; i++) {
                ctx.AddArea(areas[i].CreateTiles());
            }

            for (int i = 0; i < areas.Count; i++) {
                var area = areas[i];
                var records = area.featureRecords;

                for (int r = 0; r < records.Count; r++) {
                    var record = records[r];
                    if (record == null) {
                        // A record's class was renamed, moved, or deleted after it was baked.
                        throw new InvalidOperationException($"Area '{area.areaId}' has a feature record whose class is missing. Rebake the area.");
                    }
                    catalogue.Get(record.GetType(), area.areaId).RegisterRecord(ctx, area, record);
                }
            }
        }
    }
}
