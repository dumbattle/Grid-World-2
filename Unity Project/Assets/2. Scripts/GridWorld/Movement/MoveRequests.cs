using System.Collections.Generic;
using LPE;

namespace GridWorld {
    /// <summary>
    /// A read-only view of this tick's move requests. Enumerates without allocating.
    /// </summary>
    public readonly struct MoveRequests {
        readonly Dictionary<EntityID, MoveRequest> id2request;

        public MoveRequests(Dictionary<EntityID, MoveRequest> id2request) {
            this.id2request = id2request;
        }

        public int count => id2request.Count;

        public Dictionary<EntityID, MoveRequest>.Enumerator GetEnumerator() {
            return id2request.GetEnumerator();
        }
    }
}
