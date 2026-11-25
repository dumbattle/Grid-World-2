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
            if (!CanEnterFromAnyDirection(ctx, worldTile)) {
                Debug.LogWarning($"Grid World: {npc} is on a tile that can't be entered from any direction, so it can never step back onto it.");
            }

            for (int i = 0; i < data.addons.Count; i++) {
                var addon = data.addons[i];
                if (addon == null) {
                    // An add-on's class was renamed, moved, or deleted after it was baked.
                    throw new InvalidOperationException($"{npc} has an add-on whose class is missing. Rebake the area.");
                }
                if (addon is WanderAIData wander) {
                    CheckWander(area, wander, npc);
                }
                if (addon is PatrolAIData patrol) {
                    CheckPatrol(ctx, area, patrol, npc);
                }
            }
        }

        static void CheckWander(AreaDataSO area, WanderAIData wander, string npc) {
            var bounds = area.localBounds;
            var region = wander.region;
            bool inside = region.xMin >= bounds.xMin && region.yMin >= bounds.yMin
                       && region.xMax <= bounds.xMax && region.yMax <= bounds.yMax;
            if (!inside) {
                throw new InvalidOperationException($"{npc} has a wander region {region} that goes outside the area's bounds {bounds}.");
            }
        }

        static void CheckPatrol(WorldSetupContext ctx, AreaDataSO area, PatrolAIData patrol, string npc) {
            var bounds = area.localBounds;
            var points = patrol.patrolPoints;
            for (int i = 0; i < points.Count; i++) {
                var tile = points[i].localTile;
                if (!bounds.Contains(tile)) {
                    throw new InvalidOperationException($"{npc} has patrol point {i} at {tile}, outside the area's bounds {bounds}.");
                }
                if (!CanEnterFromAnyDirection(ctx, area.ToWorldTile(tile))) {
                    Debug.LogWarning($"Grid World: {npc} has patrol point {i} at {tile}, which can't be entered from any direction.");
                }
            }
        }

        static bool CanEnterFromAnyDirection(WorldSetupContext ctx, Vector2Int tile) {
            return ctx.CanEnter(tile, Direction4.up) || ctx.CanEnter(tile, Direction4.down)
                || ctx.CanEnter(tile, Direction4.left) || ctx.CanEnter(tile, Direction4.right);
        }

        static string Describe(AreaDataSO area, NpcData data) {
            var name = data.id.Length > 0 ? $"an NPC '{data.id}'" : "an NPC";
            return $"Area '{area.areaId}' has {name} at {data.tile}";
        }
    }
}
