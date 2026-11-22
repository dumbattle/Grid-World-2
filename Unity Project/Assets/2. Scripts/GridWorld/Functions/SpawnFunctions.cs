using System;
using System.Collections.Generic;
using LPE;
using UnityEngine;

namespace GridWorld {
    /// <summary>Creating and removing entities during the tick, such as mobs from spawn zones.</summary>
    public static class SpawnFunctions {
        /// <summary>Creates an idle entity that occupies <paramref name="tile"/>, which must be free.</summary>
        public static EntityID SpawnEntity<T>(this T ctx, EntityDefinitionSO definition, Vector2Int tile, Direction4 facing)
                    where T : IWorldFunctionContext, IPerm_SpawnEntities {
            return EntityLifecycle.Spawn(ctx.data, definition, tile, facing);
        }

        public static void AddAI<T>(this T ctx, EntityID id, EntityAI ai)
                    where T : IWorldFunctionContext, IPerm_SpawnEntities {
            EntityLifecycle.AddAI(ctx.data, id, ai);
        }

        public static IReadOnlyList<SpawnZone> GetSpawnZones<T>(this T ctx)
                    where T : IWorldFunctionContext, IPerm_SpawnEntities {
            return ctx.data.spawnZones;
        }

        /// <summary>
        /// Removes the entity from every component store and frees its tile. Its ID is never reused.
        /// The player can't be despawned.
        /// </summary>
        public static void Despawn<T>(this T ctx, EntityID id)
                    where T : IWorldFunctionContext, IPerm_DespawnEntities {
            EntityLifecycle.Despawn(ctx.data, id);
        }
    }
}
