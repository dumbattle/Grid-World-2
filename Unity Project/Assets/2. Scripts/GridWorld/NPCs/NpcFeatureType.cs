using System;
using LPE;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Spawns each baked NPC, then lets each of its add-ons register its components. Placement
    /// mistakes that would break the NPC are errors naming the area, and tiles it can never be
    /// stepped onto are warnings.
    /// </summary>
    public class NpcFeatureType : WorldFeatureType<NpcData> {
        protected override void Register(WorldSetupContext ctx, AreaDataSO area, NpcData data) {
            var npc = Describe(area, data);
            if (data.definition == null) {
                throw new InvalidOperationException($"{npc} has no entity definition.");
            }

            Direction4 facing = data.facing;
            if (facing == null) {
                throw new InvalidOperationException($"{npc} has no facing direction.");
            }

            CheckPlacement(ctx, area, data, npc);

            var id = ctx.SpawnEntity(data.definition, area.ToWorldTile(data.tile), facing);

            for (int i = 0; i < data.addons.Count; i++) {
                data.addons[i].Register(ctx, area, id);
            }
        }

        // Runs before the NPC spawns, so a failed check leaves nothing half registered.
        static void CheckPlacement(WorldSetupContext ctx, AreaDataSO area, NpcData data, string npc) {
            var bounds = area.localBounds;
            if (!bounds.Contains(data.tile)) {
                throw new InvalidOperationException($"{npc} is outside the area's bounds {bounds}.");
            }

            var worldTile = area.ToWorldTile(data.tile);
            var existing = ctx.GetEntityAt(worldTile);
            if (existing.IsValid()) {
                throw new InvalidOperationException($"{npc} is on a tile that already holds {existing}.");
            }
            if (!ctx.CanEnterFromAnyDirection(worldTile)) {
                Debug.LogWarning($"Grid World: {npc} is on a tile that can't be entered from any direction, so it can never step back onto it.");
            }

            for (int i = 0; i < data.addons.Count; i++) {
                var addon = data.addons[i];
                if (addon == null) {
                    // An add-on's class was renamed, moved, or deleted after it was baked.
                    throw new InvalidOperationException($"{npc} has an add-on whose class is missing. Rebake the area.");
                }
                addon.Validate(ctx, area, npc);
            }
        }

        static string Describe(AreaDataSO area, NpcData data) {
            var name = data.id.Length > 0 ? $"an NPC '{data.id}'" : "an NPC";
            return $"Area '{area.areaId}' has {name} at {data.tile}";
        }
    }
}
