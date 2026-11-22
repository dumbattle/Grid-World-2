using System;

namespace GridWorld {
    /// <summary>
    /// Adds each baked spawn zone to the world. The root registers it for <see cref="SpawnZoneData"/>,
    /// and a game registers another for each subclass of the data.
    /// </summary>
    public class SpawnZoneFeatureType<TData> : WorldFeatureType<TData> where TData : SpawnZoneData {
        protected override void Register(WorldSetupContext ctx, AreaDataSO area, TData data) {
            var region = data.region;
            if (data.definition == null) {
                throw new InvalidOperationException($"Area '{area.areaId}' has a spawn zone at {region} with no entity definition.");
            }
            if (data.isAIMissing) {
                throw new InvalidOperationException($"Area '{area.areaId}' has a spawn zone at {region} whose AI class is missing. Rebake the area.");
            }
            if (!Contains(area.localBounds, region)) {
                throw new InvalidOperationException($"Area '{area.areaId}' has a spawn zone at {region} that isn't inside the area's bounds {area.localBounds}.");
            }

            ctx.AddSpawnZone(data.CreateZone(area));
        }

        static bool Contains(UnityEngine.RectInt outer, UnityEngine.RectInt inner) {
            return inner.xMin >= outer.xMin && inner.yMin >= outer.yMin
                && inner.xMax <= outer.xMax && inner.yMax <= outer.yMax;
        }
    }
}
