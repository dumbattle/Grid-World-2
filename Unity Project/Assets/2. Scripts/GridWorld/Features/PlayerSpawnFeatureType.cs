using System;
using LPE;

namespace GridWorld {
    public class PlayerSpawnFeatureType : WorldFeatureType<PlayerSpawnData> {
        protected override void Register(WorldSetupContext ctx, AreaDataSO area, PlayerSpawnData data) {
            if (ctx.GetPlayer().IsValid()) {
                throw new InvalidOperationException($"Area '{area.areaId}' has a second player spawn. A world needs exactly one.");
            }
            if (data.definition == null) {
                throw new InvalidOperationException($"Area '{area.areaId}' has a player spawn with no entity definition.");
            }

            Direction4 facing = data.facing;
            if (facing == null) {
                throw new InvalidOperationException($"Area '{area.areaId}' has a player spawn with no facing direction.");
            }

            var id = ctx.SpawnEntity(data.definition, area.ToWorldTile(data.tile), facing);
            ctx.SetPlayer(id);
        }
    }
}
