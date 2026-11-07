using System;
using LPE;

namespace GridWorld {
    public static class MoveRequestFunctions {
        /// <summary>
        /// Replaces any earlier request for this entity this tick. Movement ignores requests from
        /// entities partway through a step.
        /// </summary>
        public static void RequestMove<T>(this T ctx, EntityID id, Direction4 direction, bool turnOnly = false, bool run = false)
                    where T : IWorldFunctionContext, IPerm_RequestMove {
            if (direction == null) {
                throw new ArgumentNullException(nameof(direction));
            }
            ctx.data.id2moveRequest[id] = new MoveRequest(direction, turnOnly, run);
        }
    }
}
