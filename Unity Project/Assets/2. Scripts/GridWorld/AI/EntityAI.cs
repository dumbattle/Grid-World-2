using LPE;

namespace GridWorld {
    /// <summary>
    /// An entity's AI, with its own state, stored in the <c>ai</c> component. <see cref="AISystem"/>
    /// calls <see cref="Decide"/> each tick the entity is idle or has just finished a step. Submit
    /// moves with <c>ctx.RequestMove</c>.
    /// </summary>
    public abstract class EntityAI {
        /// <summary>When true, the AI doesn't run while its entity's area is inactive.</summary>
        public bool pausesWhenInactive { get; set; } = true;

        public abstract void Decide(AIContext ctx, EntityID self);
    }
}
