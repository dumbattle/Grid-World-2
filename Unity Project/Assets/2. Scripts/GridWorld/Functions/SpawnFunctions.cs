using System;
using System.Collections.Generic;
using LPE;
using UnityEngine;

namespace GridWorld {
    /// <summary>Creating and removing entities, at startup and during the tick.</summary>
    public static class SpawnFunctions {
        /// <summary>Creates an idle entity that occupies <paramref name="tile"/>, which must be free.</summary>
        public static EntityID SpawnEntity<T>(this T ctx, EntityDefinitionSO definition, Vector2Int tile, Direction4 facing)
                    where T : IWorldFunctionContext, IPerm_SpawnEntities {
            if (definition == null) {
                throw new ArgumentNullException(nameof(definition));
            }
            if (facing == null) {
                throw new ArgumentNullException(nameof(facing));
            }

            var data = ctx.data;
            var existing = data.occupancy.Get(tile);
            if (existing.IsValid()) {
                throw new InvalidOperationException($"Can't spawn {definition.name} at {tile}: {existing} is already there.");
            }

            var id = data.entityIds.Create();
            var components = data.components;

            components.Register(components.tile, id, tile);
            components.Register(components.facing, id, facing);
            components.Register(components.definition, id, definition);
            components.Register(components.step, id, Step.Idle(tile));
            data.occupancy.Claim(tile, id);
            return id;
        }

        public static void AddAI<T>(this T ctx, EntityID id, EntityAI ai)
                    where T : IWorldFunctionContext, IPerm_SpawnEntities {
            if (ai == null) {
                throw new ArgumentNullException(nameof(ai));
            }
            var components = ctx.data.components;
            if (components.ai.Has(id)) {
                throw new InvalidOperationException($"{id} already has an AI. An entity can have only one.");
            }
            components.Register(components.ai, id, ai);
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
            var data = ctx.data;
            if (!data.entityIds.Exists(id)) {
                throw new InvalidOperationException($"Can't despawn {id}: it doesn't exist.");
            }
            if (id == data.player) {
                throw new InvalidOperationException($"Can't despawn {id}: it's the player.");
            }

            // A moving entity has already claimed its target, which is its tile.
            if (data.components.tile.TryGet(id, out var tile)) {
                data.occupancy.Release(tile, id);
            }
            data.components.RemoveAll(id);
            data.id2moveRequest.Remove(id);
            data.entityIds.Destroy(id);
        }
    }
}
