using System.Collections.Generic;
using LPE;

namespace GridWorld {
    /// <summary>
    /// One cutscene, from its build to its end: its root sequence and the entities it locks. The
    /// builder adds entities only until the cutscene starts.
    /// </summary>
    public class RunningCutscene {
        public readonly string id;

        internal ISequence root;
        internal bool started;
        internal readonly List<EntityID> entities = new List<EntityID>();

        public RunningCutscene(string id) {
            this.id = id;
        }

        public IReadOnlyList<EntityID> lockedEntities => entities;
    }
}
