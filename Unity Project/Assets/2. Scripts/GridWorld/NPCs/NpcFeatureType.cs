using System;
using LPE;

namespace GridWorld {
    /// <summary>Spawns each baked NPC, then lets each of its add-ons register its components.</summary>
    public class NpcFeatureType : WorldFeatureType<NpcData> {
        protected override void Register(WorldSetupContext ctx, AreaDataSO area, NpcData data) {
            if (data.definition == null) {
                throw new InvalidOperationException($"Area '{area.areaId}' has an NPC at {data.tile} with no entity definition.");
            }

            Direction4 facing = data.facing;
            if (facing == null) {
                throw new InvalidOperationException($"Area '{area.areaId}' has an NPC at {data.tile} with no facing direction.");
            }

            var id = ctx.SpawnEntity(data.definition, area.ToWorldTile(data.tile), facing);

            for (int i = 0; i < data.addons.Count; i++) {
                var addon = data.addons[i];
                if (addon == null) {
                    // An add-on's class was renamed, moved, or deleted after it was baked.
                    throw new InvalidOperationException($"Area '{area.areaId}' has an NPC at {data.tile} with an add-on whose class is missing. Rebake the area.");
                }
                addon.Register(ctx, area, id);
            }
        }
    }
}
