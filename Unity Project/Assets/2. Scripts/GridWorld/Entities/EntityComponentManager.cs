using System;
using System.Collections.Generic;
using LPE;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Owns the framework's component stores. <see cref="Register{T}"/> is the only way to add a
    /// component, including to a store owned by a game. Every store is listed in a cleanup
    /// registry, so a despawn removes the entity from all of them.
    /// </summary>
    public class EntityComponentManager {
        public readonly EntityComponent<Vector2Int> tile = new EntityComponent<Vector2Int>();
        public readonly EntityComponent<Direction4> facing = new EntityComponent<Direction4>();
        public readonly EntityComponent<EntityDefinitionSO> definition = new EntityComponent<EntityDefinitionSO>();
        public readonly EntityComponent<Step> step = new EntityComponent<Step>();
        public readonly EntityComponent<EntityAI> ai = new EntityComponent<EntityAI>();

        List<EntityComponentStore> stores = new List<EntityComponentStore>();

        public EntityComponentManager() {
            AddStore(tile);
            AddStore(facing);
            AddStore(definition);
            AddStore(step);
            AddStore(ai);
        }

        public int storeCount => stores.Count;

        public void Register<T>(EntityComponent<T> component, EntityID id, T value) {
            component.Add(id, value);
        }

        /// <summary>
        /// Lists a store in the cleanup registry. A game calls this once for each store in its own
        /// state, before the world loads, so despawned entities are removed from it too.
        /// </summary>
        public void AddStore(EntityComponentStore store) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }
            if (stores.Contains(store)) {
                throw new InvalidOperationException("That component store is already in the cleanup registry.");
            }
            stores.Add(store);
        }

        internal void RemoveAll(EntityID id) {
            for (int i = 0; i < stores.Count; i++) {
                stores[i].Remove(id);
            }
        }
    }
}
