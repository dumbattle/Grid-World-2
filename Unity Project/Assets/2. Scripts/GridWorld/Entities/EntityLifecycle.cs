using System;
using LPE;
using UnityEngine;

namespace GridWorld {
    // Shared by the setup functions and the runtime spawn functions, so both create and remove
    // entities the same way.
    static class EntityLifecycle {
        public static EntityID Spawn(WorldInstance data, EntityDefinitionSO definition, Vector2Int tile, Direction4 facing) {
            if (definition == null) {
                throw new ArgumentNullException(nameof(definition));
            }
            if (facing == null) {
                throw new ArgumentNullException(nameof(facing));
            }

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

        public static void AddAI(WorldInstance data, EntityID id, EntityAI ai) {
            if (ai == null) {
                throw new ArgumentNullException(nameof(ai));
            }
            var components = data.components;
            if (components.ai.Has(id)) {
                throw new InvalidOperationException($"{id} already has an AI. An entity can have only one.");
            }
            components.Register(components.ai, id, ai);
        }

        public static void Despawn(WorldInstance data, EntityID id) {
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
