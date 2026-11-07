namespace GridWorld {
    public interface IWorldFunctionContext {
        WorldInstance data { get; }
        void Init(WorldInstance world);
    }
}
