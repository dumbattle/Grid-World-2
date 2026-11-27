using System.Collections.Generic;
using LPE;

namespace GridWorld {
    /// <summary>
    /// One cutscene, from its build to its end: its root sequence and the entities it locks. The
    /// builder adds entities only until the cutscene starts.
    /// </summary>
    public class RunningCutscene {
        public readonly ICutscene cutscene;
        // For logs.
        public readonly string name;

        internal ISequence root;
        internal bool started;
        internal readonly List<EntityID> entities = new List<EntityID>();

        public RunningCutscene(ICutscene cutscene) {
            this.cutscene = cutscene;
            name = cutscene.GetType().Name;
        }

        public IReadOnlyList<EntityID> lockedEntities => entities;
    }
}
