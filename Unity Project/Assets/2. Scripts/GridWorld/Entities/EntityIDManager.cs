using System.Collections.Generic;
using LPE;

namespace GridWorld {
    /// <summary>
    /// Allocates entity IDs and tracks which still exist. An ID is a runtime handle: it's never
    /// reused and never saved.
    /// </summary>
    public class EntityIDManager {
        int nextId = 0;
        HashSet<EntityID> live = new HashSet<EntityID>();

        /// <summary>How many IDs have been created, including despawned ones.</summary>
        public int count => nextId;
        public int liveCount => live.Count;

        public EntityID Create() {
            var id = new EntityID(nextId++);
            live.Add(id);
            return id;
        }

        public bool Exists(EntityID id) {
            return live.Contains(id);
        }

        internal void Destroy(EntityID id) {
            live.Remove(id);
        }
    }
}
