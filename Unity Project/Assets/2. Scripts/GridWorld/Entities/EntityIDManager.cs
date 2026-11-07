using LPE;

namespace GridWorld {
    /// <summary>
    /// Allocates entity IDs. An ID is a runtime handle: it's never reused and never saved.
    /// </summary>
    public class EntityIDManager {
        int nextId = 0;

        public int count => nextId;

        public EntityID Create() {
            return new EntityID(nextId++);
        }
    }
}
