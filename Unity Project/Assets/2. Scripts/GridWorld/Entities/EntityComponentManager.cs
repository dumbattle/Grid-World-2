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
        // The cutscene lock. Decision systems skip entities that have it.
        public readonly EntityComponent<Marker> scripted = new EntityComponent<Marker>();
        // Entity interactions are in WorldInteractions, next to tile interactions.
        // The baked feature ID, also indexed by WorldInstance.featureId2entity.
        public readonly EntityComponent<string> featureId = new EntityComponent<string>();

        List<EntityComponentStore> stores = new List<EntityComponentStore>();

        public EntityComponentManager() {
            AddStore(tile);
            AddStore(facing);
            AddStore(definition);
            AddStore(step);
            AddStore(ai);
            AddStore(scripted);
            AddStore(featureId);
        }

        public int storeCount => stores.Count;

        public void Register<T>(EntityComponent<T> component, EntityID id, T value) {
            component.Add(id, value);
        }

        /// <summary>Removes one component, such as a marker. Does nothing if the entity doesn't have it.</summary>
        public void Unregister<T>(EntityComponent<T> component, EntityID id) {
            component.Remove(id);
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
