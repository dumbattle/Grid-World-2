namespace GridWorld {
    /// <summary>
    /// Gives a system access to a restricted set of operations, defined by the permission markers
    /// its subclass implements. Subclasses are stateless. Never cast a context to
    /// <see cref="IWorldFunctionContext"/>, because that bypasses the restrictions.
    /// </summary>
    public abstract class WorldFunctionContext : IWorldFunctionContext {
        WorldInstance world;

        WorldInstance IWorldFunctionContext.data => world;

        void IWorldFunctionContext.Init(WorldInstance world) {
            this.world = world;
        }
    }
}
