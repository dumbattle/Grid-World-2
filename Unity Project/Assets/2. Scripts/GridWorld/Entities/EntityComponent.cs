using System;
using System.Collections.Generic;
using LPE;

namespace GridWorld {
    /// <summary>
    /// Stores one component type, keyed by entity. Components are added only through
    /// <see cref="EntityComponentManager.Register{T}"/>, so <see cref="Set"/> can't create one.
    /// </summary>
    public class EntityComponent<T> {
        Dictionary<EntityID, T> id2value = new Dictionary<EntityID, T>();

        public int count => id2value.Count;

        internal void Add(EntityID id, T value) {
            id2value.Add(id, value);
        }

        public bool Has(EntityID id) {
            return id2value.ContainsKey(id);
        }

        public T Get(EntityID id) {
            return id2value[id];
        }

        public bool TryGet(EntityID id, out T value) {
            return id2value.TryGetValue(id, out value);
        }

        public void Set(EntityID id, T value) {
            if (!id2value.ContainsKey(id)) {
                throw new KeyNotFoundException($"{id} has no {typeof(T).Name} component to set. Add it with Register.");
            }

            id2value[id] = value;
        }

        // Don't call Set while enumerating: collect the IDs first.
        public Dictionary<EntityID, T>.Enumerator GetEnumerator() {
            return id2value.GetEnumerator();
        }
    }
}
