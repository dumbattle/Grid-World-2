using System;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Adds a baked interact tile's interaction to each of its tiles. The tiles are added together or
    /// not at all, so a clash leaves nothing half registered.
    /// </summary>
    public class InteractTileFeatureType : WorldFeatureType<InteractTileData> {
        protected override void Register(WorldSetupContext ctx, AreaDataSO area, InteractTileData data) {
            var name = data.id.Length > 0 ? $"an interact tile '{data.id}'" : "an interact tile";
            var tiles = data.tiles;
            var feature = $"Area '{area.areaId}' has {name} at {tiles.position}";

            if (data.cutscene == null) {
                throw new InvalidOperationException($"{feature} with no cutscene, or its cutscene's class is missing. Rebake the area.");
            }
            if (tiles.width < 1 || tiles.height < 1) {
                throw new InvalidOperationException($"{feature} with a size of {tiles.size}.");
            }

            var bounds = area.localBounds;
            if (tiles.xMin < bounds.xMin || tiles.yMin < bounds.yMin || tiles.xMax > bounds.xMax || tiles.yMax > bounds.yMax) {
                throw new InvalidOperationException($"{feature} that reaches outside the area's bounds {bounds}.");
            }

            var worldTiles = new RectInt(area.ToWorldTile(tiles.position), tiles.size);
            if (!ctx.TryAddTileInteractions(worldTiles, data.cutscene, out var clash)) {
                var local = clash - area.offset;
                throw new InvalidOperationException($"{feature} that covers {local}, which already has an interaction. A tile can have only one.");
            }
        }
    }
}
