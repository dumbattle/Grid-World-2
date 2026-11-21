using System.Collections.Generic;
using LPE;

namespace GridWorld {
    /// <summary>
    /// Runs each entity's AI in the decision phase, skipping entities partway through a step, and
    /// entities in inactive areas unless their AI opts out of pausing.
    /// </summary>
    public class AISystem {
        List<EntityID> entities = new List<EntityID>();

        public void Run(AIContext ctx) {
            entities.Clear();
            ctx.CollectAIEntities(entities);

            for (int i = 0; i < entities.Count; i++) {
                var id = entities[i];
                if (ctx.GetStepState(id) == StepState.Moving) {
                    continue;
                }

                var ai = ctx.GetAI(id);
                if (ai.pausesWhenInactive && !ctx.IsInActiveArea(ctx.GetTile(id))) {
                    continue;
                }
                ai.Decide(ctx, id);
            }
        }
    }
}
